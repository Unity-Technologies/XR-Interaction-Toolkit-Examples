/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub.Utilities
{
    public static class ContextFinder
    {
        public static List<T> FindAllContextAssets<T>() where T: ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            List<T> assets = new List<T>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    if (!assets.Contains(asset))
                    {
                        assets.Add(asset);
                    }
                }
            }

            return assets;
        }
    }
}
