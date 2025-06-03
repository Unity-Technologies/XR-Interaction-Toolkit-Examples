// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using static System.Environment;

namespace Meta.Utilities.Editor
{
    public static class BuildTools
    {
        public const int MenuPriority = 50;

        [MenuItem("Tools/Project Files/Regenerate", priority = MenuPriority, secondaryPriority = -1)]
        private static void GenerateProjectFiles()
        {
            Debug.Log("GenerateProjectFiles: CodeEditor.SetExternalScriptEditor");
            CodeEditor.SetExternalScriptEditor("code");

            Debug.Log("GenerateProjectFiles: AssetDatabase.Refresh");
            AssetDatabase.Refresh();

            Debug.Log("GenerateProjectFiles: CurrentCodeEditor.SyncAll");
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }
        private static string ExtractHintPath(string line)
        {
            var match = Regex.Match(line, "\\<HintPath\\>(.*)\\<\\/HintPath\\>");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("Tools/Project Files/Regenerate and Copy All Assemblies to Root", priority = MenuPriority, secondaryPriority = -1)]
        public static async Task GenerateProjectFilesWithAssemblies()
        {
            foreach (var csproj in Directory.EnumerateFiles(ProjectRoot, "*.csproj", SearchOption.TopDirectoryOnly))
            {
                Debug.Log($"Deleting {csproj}");
                File.Delete(csproj);
            }

            GenerateProjectFiles();

            var files = Directory.EnumerateFiles(ProjectRoot, "*.csproj").
                Select(f => File.ReadAllLinesAsync(f)).
                ToArray();
            Debug.Log($"{files.Length} csproj files found.");

            var fileLines = await Task.WhenAll(files);
            Debug.Log($"All csproj files loaded.");

            var paths = fileLines.
                SelectMany(l => l).
                Select(ExtractHintPath).
                Where(path => path != null).
                Distinct().
                ToArray();
            Debug.Log($"{paths.Length} dll files found.");

            var referenceAssembliesPath = Path.Combine(ProjectRoot, "ReferenceAssemblies");
            if (Directory.Exists(referenceAssembliesPath))
            {
                Debug.Log($"Deleting {referenceAssembliesPath}");
                Directory.Delete(referenceAssembliesPath, true);
            }
            Debug.Log($"Creating {referenceAssembliesPath}");
            _ = Directory.CreateDirectory(referenceAssembliesPath);

            foreach (var path in paths)
            {
                var name = Path.Combine(referenceAssembliesPath, Path.GetFileName(path));
                if (!File.Exists(name))
                {
                    Debug.Log($"Copying {name}");
                    File.Copy(path, name, false);
                }
            }
        }

        public static async void GenerateProjectFilesWithAssembliesAndQuit()
        {
            await GenerateProjectFilesWithAssemblies().
                ContinueWith(_ => Application.Quit(0));
        }

        private static string KeystorePasswordFilePath => Path.Combine(ProjectRoot, "keystore-passwords.txt");

        [InitializeOnLoadMethod]
        public static async Task SetKeystorePassword()
        {
            var lines = await File.ReadAllLinesAsync(KeystorePasswordFilePath);
            if (lines != null)
            {
                var keystorePass = lines.Length > 0 ? lines[0].Trim() : PlayerSettings.Android.keystorePass;
                var keyaliasPass = lines.Length > 1 ? lines[1].Trim() : PlayerSettings.Android.keyaliasPass;
                if (keystorePass != PlayerSettings.Android.keystorePass || keyaliasPass != PlayerSettings.Android.keyaliasPass)
                {
                    PlayerSettings.Android.keystorePass = keystorePass;
                    PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    Debug.Log("Keystore passwords set from file.");
                }
            }
        }

        [MenuItem("Tools/Keystore/Set passwords from file", priority = MenuPriority)]
        public static async void SetKeystorePasswordMenu()
        {
            if (!File.Exists(KeystorePasswordFilePath))
            {
                if (EditorUtility.DisplayDialog("Set keystore passwords from file", "Open keystore-passwords.txt?", "OK"))
                {
                    {
                        using var file = File.CreateText(KeystorePasswordFilePath);
                    }
                    _ = System.Diagnostics.Process.Start(KeystorePasswordFilePath);
                }
                return;
            }
            await SetKeystorePassword();
        }

        public static async Task<BuildResult> BuildAndroid()
        {
            var keystorePass = GetEnvironmentVariable("UNITY_KEYSTORE_PASSWORD");
            if (!string.IsNullOrEmpty(keystorePass))
            {
                Debug.Log("Setting keystore password from $UNITY_KEYSTORE_PASSWORD");
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.useCustomKeystore = true;
            }
            else
            {
                Debug.Log("$UNITY_KEYSTORE_PASSWORD is not set");
                PlayerSettings.Android.useCustomKeystore = false;
            }

            var keyaliasPass = GetEnvironmentVariable("UNITY_KEYALIAS_PASSWORD");
            if (!string.IsNullOrEmpty(keyaliasPass))
            {
                Debug.Log("Setting keyalias password from $UNITY_KEYALIAS_PASSWORD");
                PlayerSettings.Android.keyaliasPass = keyaliasPass;
            }
            else
            {
                Debug.Log("$UNITY_KEYALIAS_PASSWORD is not set");
            }

            var apkVersionStr = GetEnvironmentVariable("UNITY_APK_VERSION");
            if (!string.IsNullOrEmpty(apkVersionStr) && int.TryParse(apkVersionStr, out var apkVersion))
            {
                Debug.Log($"Setting apk version from $UNITY_APK_VERSION to {apkVersion}");
                PlayerSettings.Android.bundleVersionCode = apkVersion;
                PlayerSettings.bundleVersion = $"0.1.{apkVersion:d5}";
            }
            else
            {
                Debug.Log("$UNITY_APK_VERSION is not set");
            }

            var androidSdk = GetEnvironmentVariable("UNITY_ANDROID_SDK");
            if (!string.IsNullOrEmpty(androidSdk))
            {
                EditorPrefs.SetString("AndroidSdkRoot", androidSdk);
                Debug.Log($"AndroidSdkRoot={androidSdk}");
            }

            var androidNdk = GetEnvironmentVariable("UNITY_ANDROID_NDK");
            if (!string.IsNullOrEmpty(androidNdk))
            {
                EditorPrefs.SetString("AndroidNdkRootR21D", androidNdk);
                Debug.Log($"AndroidNdkRootR21D={androidNdk}");
            }

            await RunHashFixerOnPrefabs();

            // fix for an issue in which VFX assets aren't always being imported for Android builds
            ReimportVFX();

            var sceneList = EditorBuildSettings.scenes.
                Where(s => s.enabled).
                Select(s => s.path).
                ToArray();

            var reimportScenes = GetEnvironmentVariable("REIMPORT_SCENES");
            if (!string.IsNullOrEmpty(reimportScenes))
            {
                ReimportScenes(sceneList);
            }

            var buildOptions = BuildOptions.None;

            var developmentBuild = GetEnvironmentVariable("UNITY_DEVELOPMENT_BUILD");
            if (!string.IsNullOrEmpty(developmentBuild))
            {
                buildOptions |= BuildOptions.Development;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = sceneList.ToArray(),
                locationPathName = ".\\build\\QuestSocialGameplay.apk",
                target = BuildTarget.Android,
                options = buildOptions
            };

            var buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var steps = buildResult.steps.Select(BuildStepToString).ListToString("\n");
            var summary = buildResult.summary;
            var summaryObj = new
            {
                summary.result,
                summary.buildStartedAt,
                summary.guid,
                summary.platform,
                summary.platformGroup,
                summary.options,
                summary.outputPath,
                summary.totalSize,
                summary.totalTime,
                summary.buildEndedAt,
                summary.totalErrors,
                summary.totalWarnings,
            };
            Debug.Log($@"
===
Build complete. Result: {summary.result}
===
Summary:
{summaryObj}
===
Steps:
{steps}
===");
            return summary.result;
        }

        public static async void BuildAndroidAndQuit()
        {
            var result = await BuildAndroid();
            var code = result switch
            {
                BuildResult.Succeeded => 0,
                BuildResult.Unknown => -1,
                _ => (int)result,
            };
            Application.Quit(code);
        }

        private static string BuildStepToString(BuildStep step)
        {
            var messages = step.messages.
                Select(f => $"[{f.type}] {f.content}").
                ListToString("\t\n");
            return $"Build Step '{step.name}' ({step.duration}).\t\n{messages}";
        }

        private static async Task RunHashFixerOnPrefabs()
        {
            var fixer = GetType("NetcodeHashFixer");
            var fixMethod = fixer?.GetMethod("RegenerateAllNetworkPrefabHashIds");
            var fixMethodReturn = fixMethod?.Invoke(null, System.Array.Empty<object>());
            if (fixMethodReturn is Task fixMethodTask)
                await fixMethodTask;
        }

        private static System.Type GetType(string fullTypeName) =>
            System.AppDomain.CurrentDomain.GetAssemblies().
                SelectMany(GetTypes).
                FirstOrDefault(t => t.FullName == fullTypeName);

        private static System.Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return System.Array.Empty<System.Type>();
            }
        }

        private static void ReimportScenes(string[] sceneList)
        {
            foreach (var scene in sceneList)
            {
                Debug.Log($"Reimporting: {scene}");
                AssetDatabase.ImportAsset(scene, ImportAssetOptions.ForceUpdate);
            }
        }

        [MenuItem("Tools/VFX/Reimport", priority = MenuPriority)]
        private static void ReimportVFX()
        {
            var guids = AssetDatabase.FindAssets("t:visualeffectasset t:shadergraphvfxasset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"Reimporting '{path}'");
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ImportRecursive | ImportAssetOptions.DontDownloadFromCacheServer);
            }
            Debug.Log("ReimportVFX complete");
        }
    }
}
