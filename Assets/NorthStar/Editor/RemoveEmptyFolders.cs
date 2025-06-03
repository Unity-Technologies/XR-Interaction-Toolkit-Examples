// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace NorthStar
{
    /// <summary>
    /// Clean the project of emprt folders
    /// </summary>
    public class RemoveEmptyFolders
    {
        [MenuItem("Tools/NorthStar/Clean Empty Folders")]
        private static void OnMenuShow()
        {
            // Get all directories 
            var directoryInfo = new DirectoryInfo(Application.dataPath); // Points to asset directory in editor
            foreach (var subDirectory in directoryInfo.GetDirectories("*.*", SearchOption.AllDirectories))
            {
                var hasNonMetaFiles = false;
                foreach (var file in subDirectory.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    // If there are any non-meta files, this folder isn't empty
                    if (!file.FullName.EndsWith(".meta"))
                    {
                        hasNonMetaFiles = true;
                        break;
                    }
                }

                // Don't delete non empty folders
                if (hasNonMetaFiles)
                    continue;

                Debug.Log($"{subDirectory.FullName} deleted");

                // Delete the folder
                Assert.IsTrue(FileUtil.DeleteFileOrDirectory(subDirectory.FullName));

                // Also need to delete the meta file
                Assert.IsTrue(FileUtil.DeleteFileOrDirectory($"{subDirectory.Parent}/{subDirectory.Name}.meta"));
            }

            // Required to make the deleted files disappear from the project window without a manual refresh
            AssetDatabase.Refresh();
        }
    }
}