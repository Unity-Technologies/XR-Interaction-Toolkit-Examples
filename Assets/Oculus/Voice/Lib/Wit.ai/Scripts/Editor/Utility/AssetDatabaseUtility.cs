/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Utilities
{
    public static class AssetDatabaseUtility
    {
        // Find Unity asset
        public static T FindUnityAsset<T>(string filter) where T : UnityEngine.Object
        {
            T[] results = FindUnityAssets<T>(filter, true);
            if (results != null && results.Length > 0)
            {
                return results[0];
            }
            return null;
        }

        // Get all unity objects matching the name
        public static T[] FindUnityAssets<T>(string filter, bool ignoreAdditional = false)
            where T : UnityEngine.Object
        {
            List<T> results = new List<T>();
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids != null && guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (asset != null && !results.Contains(asset))
                    {
                        results.Add(asset);
                        if (ignoreAdditional)
                        {
                            break;
                        }
                    }
                }
            }
            return results.ToArray();
        }

        // Gets the absolute string path of the asset(s) with the matching name
        public static string[] FindUnityAssetPath(string filter, bool ignoreAdditional = false)
        {
            var relativePaths = AssetDatabase.FindAssets(filter);
            char d = Path.DirectorySeparatorChar;

            List<string> results = new List<string>();
            foreach (var relPath in relativePaths)
            {
                results.Add($"{Application.dataPath}{d}..{d}{AssetDatabase.GUIDToAssetPath(relPath)}");
                if (ignoreAdditional)
                {
                    break;
                }
            }
            return results.ToArray();
        }
    }
}
