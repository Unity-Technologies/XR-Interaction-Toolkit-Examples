/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.WitAi.Utilities
{
    public static class GameObjectSearchUtility
    {
        /// <summary>
        /// Finds the first available scene scripts of a specific object type
        /// </summary>
        /// <param name="includeInactive">Whether inactive GameObjects should be searched</param>
        /// <typeparam name="T">Script type being searched</typeparam>
        /// <returns>The first found script matching the specified type</returns>
        public static T FindSceneObject<T>(bool includeInactive = true) where T : UnityEngine.Object
        {
            T[] results = FindSceneObjects<T>(includeInactive, true);
            return results == null || results.Length == 0 ? null : results[0];
        }
        /// <summary>
        /// Finds all scene scripts of a specific object type
        /// </summary>
        /// <param name="includeInactive">Whether inactive GameObjects should be searched</param>
        /// <param name="returnImmediately">Whether the method should return as soon as a matching script is found</param>
        /// <typeparam name="T">Script type being searched</typeparam>
        /// <returns>All scripts matching the specified type</returns>
        public static T[] FindSceneObjects<T>(bool includeInactive = true, bool returnImmediately = false) where T : UnityEngine.Object
        {
            // Use default functionality
            if (!includeInactive)
            {
                return GameObject.FindObjectsOfType<T>();
            }

            // Get results
            List<T> results = new List<T>();

            // Iterate loaded scenes
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                // Iterate root
                foreach (var rootGameObject in SceneManager.GetSceneAt(s).GetRootGameObjects())
                {
                    T[] children = rootGameObject.GetComponentsInChildren<T>(includeInactive);
                    if (children != null && children.Length > 0)
                    {
                        results.AddRange(children);
                        if (returnImmediately)
                        {
                            return results.ToArray();
                        }
                    }
                }
            }

            // Return all
            return results.ToArray();
        }
    }
}
