/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor.PackageManager;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class PackageCleanup
    {
        private enum CleanupOperation
        {
            None,
            Delete,
            Move,
            StripTags,
        }

        private class CleanupInfo
        {
            public CleanupOperation Operation;
            public GUID AssetGuid;
            public GUID MoveToPathGuid;
        }

        private enum CleanupResult
        {
            None,
            Success,
            Cancel,
            Incomplete,
        }

        public const string PACKAGE_VERSION = "0.54.0";
        public const string DEPRECATED_TAG = "oculus_interaction_deprecated";
        public const string MOVED_TAG = "oculus_interaction_moved_";
        private const string MENU_NAME = "Oculus/Interaction/Clean Up Package";
        private const string AUTO_CLEANUP_KEY = "Oculus_Interaction_AutoCleanUp_" + PACKAGE_VERSION;

        private static bool AutoCleanup
        {
            get => PlayerPrefs.GetInt(AUTO_CLEANUP_KEY, 1) == 1;
            set => PlayerPrefs.SetInt(AUTO_CLEANUP_KEY, value ? 1 : 0);
        }

        static PackageCleanup()
        {
            EditorApplication.delayCall += HandleDelayCall;
        }

        [MenuItem(MENU_NAME)]
        private static void AssetRemovalMenuCommand()
        {
            AutoCleanup = true;
            StartRemovalUserFlow(true);
        }

        private static void HandleDelayCall()
        {
            bool startAutoDeprecation = !Application.isBatchMode &&
                                        AutoCleanup &&
                                        !Application.isPlaying;
            if (startAutoDeprecation)
            {
                StartRemovalUserFlow(false);
            }
        }

        /// <summary>
        /// Check if there are any assets in the project that require
        /// cleanup operations.
        /// </summary>
        /// <returns>True if package needs cleanup</returns>
        public static bool CheckPackageNeedsCleanup()
        {
            return GetAssetInfos().Count > 0;
        }

        /// <summary>
        /// Start the removal flow for removing deprecated assets.
        /// </summary>
        /// <param name="userTriggered">If true, the window will
        /// be non-modal, and a dialog will be shown if no assets found</param>
        public static void StartRemovalUserFlow(bool userTriggered)
        {
            var assetInfos = GetAssetInfos();

            if (assetInfos.Count == 0)
            {
                if (userTriggered)
                {
                    EditorUtility.DisplayDialog("Interaction SDK",
                        "No clean up needed in package.", "Close");
                }
                else
                {
                    return;
                }
            }
            else
            {
                int deletionPromptResult = EditorUtility.DisplayDialogComplex(
                    "Interaction SDK",
                    "This utility performs a cleanup operation which relocates " +
                    "Interaction SDK files and folders, and removes asset stubs provided " +
                    "for backwards compatibility during package upgrade." +
                    "\n\n" +
                    "Click 'Show Assets' to view a list of the assets to be modified. " +
                    "You will then be given the option to run the cleanup operation on them.",
                    "Show Assets (Recommended)", "No, Don't Ask Again", "No");

                switch (deletionPromptResult)
                {
                    case 0: // "Yes"
                        bool modalWindow = !userTriggered;
                        ShowAssetCleanupWindow(assetInfos, modalWindow);
                        break;
                    case 1: // "No, Don't Ask Again"
                        AutoCleanup = false;
                        ShowCancelDialog();
                        break;
                    default:
                    case 2: // "No"
                        AutoCleanup = true;
                        break;
                }
            }
        }

        private static IReadOnlyList<CleanupInfo> GetAssetInfos()
        {
            List<CleanupInfo> result = new List<CleanupInfo>();

            var deprecatedGUIDs = AssetDatabase.FindAssets($"l:{DEPRECATED_TAG}", null)
                .Select((guidStr) => new GUID(guidStr));
            var movedGUIDs = AssetDatabase.FindAssets($"l:{MOVED_TAG}", null)
                .Select((guidStr) => new GUID(guidStr));

            foreach (var GUID in deprecatedGUIDs)
            {
                result.Add(new CleanupInfo()
                {
                    Operation = CleanupOperation.Delete,
                    AssetGuid = GUID,
                });
            }

            foreach (var GUID in movedGUIDs)
            {
                if (GetDestFolderForMovedAsset(GUID, out GUID newPathGUID))
                {
                    result.Add(new CleanupInfo()
                    {
                        Operation = CleanupOperation.Move,
                        AssetGuid = GUID,
                        MoveToPathGuid = newPathGUID,
                    });
                }
                else
                {
                    result.Add(new CleanupInfo()
                    {
                        Operation = CleanupOperation.StripTags,
                        AssetGuid = GUID,
                    });
                }
            }

            result.RemoveAll((info) =>
            {
                // Ignore assets in read-only packages
                var pSource = PackageInfo.FindForAssetPath(
                    AssetDatabase.GUIDToAssetPath(info.AssetGuid))?.source;
                return pSource != null && // In Assets folder
                       pSource != PackageSource.Embedded &&
                       pSource != PackageSource.Local;
            });

            return result;
        }

        private static void ShowAssetCleanupWindow(
            IEnumerable<CleanupInfo> cleanupInfos, bool modal)
        {
            void DrawHeader(AssetListWindow window)
            {
                EditorGUILayout.HelpBox(
                    "Assets marked Delete will be permanently deleted",
                    MessageType.Warning);
            }

            void DrawFooter(AssetListWindow window)
            {
                GUILayoutOption buttonHeight = GUILayout.Height(36);
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Clean Up (Recommended)", buttonHeight))
                {
                    var result = CleanUpAssets(cleanupInfos);
                    switch (result)
                    {
                        default:
                        case CleanupResult.None:
                        case CleanupResult.Cancel:
                            AutoCleanup = true;
                            break;
                        case CleanupResult.Success:
                        case CleanupResult.Incomplete:
                            AutoCleanup = false;
                            window.Close();
                            break;
                    }
                }
                if (GUILayout.Button("Cancel", buttonHeight))
                {
                    ShowCancelDialog();
                }
                EditorGUILayout.EndHorizontal();
            }

            List<AssetListWindow.AssetInfo> windowInfos =
                new List<AssetListWindow.AssetInfo>();

            foreach (var info in cleanupInfos)
            {
                switch (info.Operation)
                {
                    default:
                    case CleanupOperation.None:
                        break;
                    case CleanupOperation.Delete:
                        windowInfos.Add(new AssetListWindow.AssetInfo(
                            GUIDToAssetPath(info.AssetGuid),
                            $"<color=orange>Delete:</color> " +
                            $"{GUIDToAssetPath(info.AssetGuid)}"));
                        break;
                    case CleanupOperation.Move:
                        windowInfos.Add(new AssetListWindow.AssetInfo(
                            GUIDToAssetPath(info.AssetGuid),
                            $"<color=yellow>Move:</color> " +
                            $"{GUIDToAssetPath(info.AssetGuid)} -> " +
                            $"{GUIDToAssetPath(info.MoveToPathGuid)}"));
                        break;
                    case CleanupOperation.StripTags:
                        windowInfos.Add(new AssetListWindow.AssetInfo(
                            GUIDToAssetPath(info.AssetGuid),
                            $"<color=lime>Unlabel:</color> " +
                            $"{GUIDToAssetPath(info.AssetGuid)}"));
                        break;
                }
            }

            AssetListWindow assetListWindow = AssetListWindow.Show(
                "Interaction SDK - All Assets to be Modified",
                windowInfos, modal, DrawHeader, DrawFooter);
        }

        private static void ShowCancelDialog()
        {
            AssetListWindow.CloseAll();
            EditorUtility.DisplayDialog("Interaction SDK",
                $"Package cleanup was not run. " +
                $"You can run this utility at any time " +
                $"using the '{MENU_NAME}' menu.",
                "Close");
        }

        private static bool GetDestFolderForMovedAsset(GUID assetGUID, out GUID destFolderGUID)
        {
            destFolderGUID = new GUID();

            Object assetObject = AssetDatabase.LoadMainAssetAtPath(GUIDToAssetPath(assetGUID));
            List<string> labels = new List<string>(AssetDatabase.GetLabels(assetObject));

            int index = labels.FindIndex((l) => l.Contains(MOVED_TAG));
            if (index >= 0)
            {
                destFolderGUID = new GUID(labels[index].Remove(0, MOVED_TAG.Length));

                // Verify that paths exist, and new path is not the same as old path
                string curPath = Path.GetFullPath(GUIDToAssetPath(assetGUID));
                string newFolder = Path.GetFullPath(GUIDToAssetPath(destFolderGUID));
                string targetFilePath = Path.Combine(newFolder, Path.GetFileName(curPath));

                if (!curPath.Equals(targetFilePath) &&
                    (Directory.Exists(curPath) || File.Exists(curPath)) &&
                    Directory.Exists(newFolder))
                {
                    return true;
                }
            }

            return false;
        }

        private static CleanupResult CleanUpAssets(IEnumerable<CleanupInfo> cleanupInfos)
        {
            if (EditorUtility.DisplayDialog("Are you sure?",
                "Any assets marked for deletion will be permanently deleted." +
                "\n\n" +
                "It is strongly recommended that you back up your project before proceeding.",
                "Clean Up Package", "Cancel"))
            {
                var deletions = new List<GUID>();
                var moves = new Dictionary<GUID, GUID>();
                var stripTags = new List<GUID>();

                foreach (var info in cleanupInfos)
                {
                    switch (info.Operation)
                    {
                        default:
                        case CleanupOperation.None:
                            break;
                        case CleanupOperation.Delete:
                            deletions.Add(info.AssetGuid);
                            break;
                        case CleanupOperation.Move:
                            moves.Add(info.AssetGuid, info.MoveToPathGuid);
                            break;
                        case CleanupOperation.StripTags:
                            stripTags.Add(info.AssetGuid);
                            break;
                    }
                }

                bool result = true;
                result &= MoveAssets(moves);
                result &= DeleteAssets(deletions);
                result &= StripTags(stripTags);
                return result ? CleanupResult.Success : CleanupResult.Incomplete;
            }
            else
            {
                return CleanupResult.Cancel;
            }
        }

        private static bool MoveAssets(IDictionary<GUID, GUID> curToNewPathGUID)
        {
            Dictionary<string, string> moves = new Dictionary<string, string>();
            Dictionary<string, string> failures = new Dictionary<string, string>();

            foreach (var assetGUID in curToNewPathGUID.Keys)
            {
                if (!curToNewPathGUID.TryGetValue(assetGUID, out GUID newPathGUID))
                {
                    string failedPath = GUIDToAssetPath(assetGUID);
                    failures.Add(failedPath, $"No new path provided for asset {failedPath}");
                    continue;
                }

                string curPath = GUIDToAssetPath(assetGUID);
                string newPath = Path.Combine(GUIDToAssetPath(newPathGUID),
                    Path.GetFileName(curPath));

                if (Path.GetFullPath(curPath).Equals(Path.GetFullPath(newPath)))
                {
                    // Source and destination paths already match
                    continue;
                }

                string result = AssetDatabase.MoveAsset(curPath, newPath);

                if (!string.IsNullOrEmpty(result))
                {
                    failures.Add(curPath, result);
                }
                else
                {
                    // Strip labels after successful move
                    StripTag(assetGUID, MOVED_TAG);
                    moves.Add(curPath, newPath);
                }
            }

            string logMessage;
            if (BuildLogMessage("Assets moved:",
                moves.Keys.Select((key) => $"{key} -> {moves[key]}"),
                out logMessage))
            {
                Debug.Log(logMessage);
            }
            if (BuildLogMessage("Could not move assets:",
                failures.Keys.Select((key) => $"{key}:{failures[key]}"),
                out logMessage))
            {
                Debug.LogError(logMessage);
            }
            return failures.Count == 0;
        }

        private static bool DeleteAssets(IEnumerable<GUID> assetGUIDs)
        {
            var assetPaths = assetGUIDs
                .Select((guid) => GUIDToAssetPath(guid));

            HashSet<string> filesToDelete = new HashSet<string>();
            HashSet<string> foldersToDelete = new HashSet<string>();
            HashSet<string> skippedFolders = new HashSet<string>();
            HashSet<string> failedPaths = new HashSet<string>();

            foreach (var path in assetPaths)
            {
                if (File.Exists(path))
                {
                    filesToDelete.Add(path);
                }
                else if (Directory.Exists(path))
                {
                    foldersToDelete.Add(path);
                }
                else
                {
                    failedPaths.Add(path);
                }
            }

#if UNITY_2020_1_OR_NEWER
            List<string> failed = new List<string>();

            // Delete files
            AssetDatabase.DeleteAssets(filesToDelete.ToArray(), failed);
            failedPaths.UnionWith(failed);

            // Remove non-empty folders from delete list
            skippedFolders.UnionWith(foldersToDelete
                .Where((path) => AssetDatabase.FindAssets("", new[] { path })
                .Select((guid) => AssetDatabase.GUIDToAssetPath(guid))
                .Any((path) => !AssetDatabase.IsValidFolder(path))));
            foldersToDelete.ExceptWith(skippedFolders);

            // Delete folders, removing longest paths (subfolders) first
            List<string> sortedFolders = new List<string>(foldersToDelete);
            sortedFolders.Sort((a, b) => b.Length.CompareTo(a.Length));
            AssetDatabase.DeleteAssets(sortedFolders.ToArray(), failed);
            failedPaths.UnionWith(failed);
#else
            // Delete files
            foreach (var path in filesToDelete)
            {
                if (!AssetDatabase.DeleteAsset(path))
                {
                    failedPaths.Add(path);
                }
            }

            // Remove non-empty folders from delete list
            skippedFolders.UnionWith(foldersToDelete
                .Where((path) => Directory.EnumerateFiles(path).Any()));
            foldersToDelete.ExceptWith(skippedFolders);

            // Delete folders
            foreach (var path in foldersToDelete)
            {
                if (!AssetDatabase.DeleteAsset(path))
                {
                    failedPaths.Add(path);
                }
            }
#endif
            string logMessage;

            if (BuildLogMessage("Deprecated assets deleted:",
                filesToDelete.Union(foldersToDelete), out logMessage))
            {
                Debug.Log(logMessage);
            }
            if (BuildLogMessage("Skipped non-empty folders:",
                skippedFolders, out logMessage))
            {
                Debug.LogWarning(logMessage);
            }
            if (BuildLogMessage("Failed to delete assets:",
                failedPaths, out logMessage))
            {
                Debug.LogError(logMessage);
            }

            return failedPaths.Count == 0;
        }

        private static bool StripTags(IEnumerable<GUID> assetGUIDs)
        {
            foreach (var GUID in assetGUIDs)
            {
                StripTag(GUID, DEPRECATED_TAG);
                StripTag(GUID, MOVED_TAG);
            }
            return true;
        }

        private static void StripTag(in GUID assetGUID, string tag)
        {
            string assetPath = GUIDToAssetPath(assetGUID);
            Object assetObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
            List<string> labels = new List<string>(AssetDatabase.GetLabels(assetObject));
            labels.RemoveAll((l) => l.Contains(tag));
            AssetDatabase.SetLabels(assetObject, labels.ToArray());
        }

        private static bool BuildLogMessage(
            string title,
            IEnumerable<string> messages,
            out string message)
        {
            int count = 0;
            StringBuilder sb = new StringBuilder();

            sb.Append(title);
            foreach (var msg in messages)
            {
                sb.Append(System.Environment.NewLine);
                sb.Append(msg);
                ++count;
            }
            message = sb.ToString();
            return count > 0;
        }

        private static string GUIDToAssetPath(GUID guid)
        {
#if UNITY_2020_3_OR_NEWER
            return AssetDatabase.GUIDToAssetPath(guid);
#else
            return AssetDatabase.GUIDToAssetPath(guid.ToString());
#endif
        }
    }
}
