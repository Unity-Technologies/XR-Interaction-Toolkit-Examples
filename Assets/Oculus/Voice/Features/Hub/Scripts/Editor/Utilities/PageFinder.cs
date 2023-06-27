/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.Voice.Hub.Attributes;
using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub.Utilities
{
    internal static class PageFinder
    {
        private static List<Type> _pages;
        internal static List<Type> FindPages()
        {
            if (null == _pages)
            {
                _pages = ReflectionUtils.GetTypesWithAttribute<MetaHubPageAttribute>();
            }

            return _pages;
        }

        internal static MetaHubPageAttribute GetPageInfo(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(MetaHubPageAttribute), false);
            return attributes.Length > 0 ? (MetaHubPageAttribute) attributes[0] : null;
        }

        internal static List<ScriptableObject> FindPages(Type t)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(t))
            {
                throw new ArgumentException("The specified type must be a ScriptableObject.");
            }

            return FindPages(t.Name);
        }

        public static List<ScriptableObject> FindPages(string type)
        {
            List<ScriptableObject> pages = new List<ScriptableObject>();
            string[] guids = AssetDatabase.FindAssets($"t:{type}");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                {
                    pages.Add(asset);
                }
            }

            return pages;
        }
    }
}
