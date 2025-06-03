// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_SEARCH_EXTENSIONS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Meta.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.EditorUtility;

namespace Meta.Utilities.Editor
{
    public static class MenuHelpers
    {
        private static Type DependencyGraphViewerType { get; } = Type.GetType("UnityEditor.Search.DependencyGraphViewer, com.unity.search.extensions.editor");

        [MenuItem("Assets/Dependencies/Graph Dependencies", true)]
        public static bool GraphDependencies_Validate() => DependencyGraphViewerType != null;

        [MenuItem("Assets/Dependencies/Graph Dependencies", priority = 10110)]
        public static void GraphDependencies()
        {
            var createWindow = typeof(EditorWindow).GetMethod(
                    nameof(EditorWindow.CreateWindow),
                    new[] { typeof(Type[]) }
                ).
                MakeGenericMethod(DependencyGraphViewerType);
            var win = (EditorWindow)createWindow.Invoke(null, new[] { new Type[0] });
            win.Show();
            var import = win.GetMethod<Action<ICollection<UnityEngine.Object>>>("Import");
            EditorApplication.delayCall += () => import.Invoke(Selection.objects);
        }

        [MenuItem("Tools/Clean-up/Identify Missing References (via git)", priority = BuildTools.MenuPriority)]
        public static async void IdentifyMissingReferences()
        {
            var source = new TaskCompletionSource<IList<SearchItem>>();
            var provider = SearchService.Providers.FirstOrDefault(p => p.name is "Dependencies");
            var context = new SearchContext(new[] { provider }, "is:broken");
            SearchService.Request(context, (_, results) => source.SetResult(results), SearchFlags.Default | SearchFlags.WantsMore);

            var results = await source.Task;

            var binding = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var getDescription = typeof(Dependency).GetMethod("GetDescription", binding).
                CreateDelegate(typeof(Func<SearchItem, string>)) as Func<SearchItem, string>;
            var prefix = "Broken links ";
            var brokenLinks = results.Where(r => r.ToObject() != null).ToDictionary(
                r => r.ToObject(),
                r => getDescription(r)[prefix.Length..].Split(", "));

            var guidsToFiles = await GetGuidsToFiles(brokenLinks.SelectMany(g => g.Value));
            var values = brokenLinks.
                SelectMany(pair => pair.Value.
                    Select(guid => (
                        obj: pair.Key,
                        guid,
                        file: guidsToFiles.TryGetValue(guid, out var file) ? file.Trim() : null
                    ))).
                Distinct().
                ToLookup(data => data.file != null);

            foreach (var (obj, guid, file) in values[true])
            {
                var property = await GetProperty(obj, guid);
                Debug.Log($"{obj.name}.{property} = {file} ({guid})", obj);
            }

            foreach (var (obj, guid, file) in values[false])
            {
                var property = await GetProperty(obj, guid);
                Debug.Log($"{obj.name}.{property} = unknown file ({guid})", obj);
            }
        }

        private static async Task<string> GetProperty(UnityEngine.Object obj, string guid)
        {
            var propertyRegex = new Regex($"propertyPath: (.*)\n *value: (.*)\n *objectReference: .*{guid}.*");
            var otherRegex = new Regex($"[ -]*(.*?):[^:]*{{.*{guid}");
            var yaml = await File.ReadAllTextAsync(AssetDatabase.GetAssetPath(obj));
            if (yaml.Length > 1024 * 1024 * 1024)
                return "";
            var matches = await Task.WhenAll(new[]
            {
                Task.Factory.StartNew(() => propertyRegex.Match(yaml),
                    default, default, TaskScheduler.Default),
                Task.Factory.StartNew(() => otherRegex.Match(yaml),
                    default, default, TaskScheduler.Default),
            });
            return matches?.
                SelectMany(m => m.Groups.Skip(1))?.
                FirstOrDefault(s => s?.Length > 0)?.
                ToString() ?? "";
        }

        private static async Task<Dictionary<string, string>> GetGuidsToFiles(IEnumerable<string> guids)
        {
            var allIdsRegex = guids.
                WhereNonNull().
                Select(guid => $"({guid})").
                Distinct().
                ListToString("|");
            var proc = new System.Diagnostics.Process
            {
                EnableRaisingEvents = true,
                StartInfo = new()
                {
                    FileName = "cmd",
                    Arguments = $"/c \"git log -G\\\"{allIdsRegex}\\\" --color=never --pretty=format: -U0 -- *.meta \"",
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

            if (proc.Start())
            {
                var fileRegex = new Regex(@"^\+\+\+ b\/(.*)$", RegexOptions.Compiled);
                var guidRegex = new Regex(@"^\+guid: ([a-f0-9]*)$", RegexOptions.Compiled);
                var currentFile = null as string;
                var guidToFile = new Dictionary<string, string>();
                while (true)
                {
                    var result = await proc.StandardOutput.ReadLineAsync();
                    if (result == null)
                        break;

                    if (GetRegexCapture(fileRegex, result) is { } file)
                    {
                        currentFile = file;
                    }
                    else if (GetRegexCapture(guidRegex, result) is { } guid)
                    {
                        _ = guidToFile.TryAdd(guid, currentFile);
                    }
                }

                return guidToFile;
            }

            return null;
        }

        private static string GetRegexCapture(Regex fileRegex, string result) => fileRegex.Match(result)?.Groups?.Skip(1)?.FirstOrDefault()?.Value;

        [MenuItem("Tools/Clean-up/Fix Incorrect Asset Names", priority = BuildTools.MenuPriority)]
        public static async void FixIncorrectAssetNames()
        {
            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/")).ToArray();
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var (i, assetPath) in assetPaths.Enumerate())
                {
                    if (DisplayCancelableProgressBar("Fixing incorrect asset names...", assetPath, i * 1.0f / assetPaths.Length))
                        break;

                    if (!assetPath.EndsWith("asset") && !assetPath.EndsWith("prefab"))
                        continue;

                    var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (asset == null)
                        continue;

                    var mainObjectName = asset.name;
                    var expectedMainObjectName = Path.GetFileNameWithoutExtension(assetPath);

                    if (mainObjectName != expectedMainObjectName)
                    {
                        Debug.Log($"Fixing object '{assetPath}' (renaming from '{mainObjectName}' to '{expectedMainObjectName}'", asset);
                        asset.name = expectedMainObjectName;
                        AssetDatabase.SaveAssetIfDirty(asset);
                    }

                    await Task.Yield();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                ClearProgressBar();
            }
        }
    }
}

#endif
