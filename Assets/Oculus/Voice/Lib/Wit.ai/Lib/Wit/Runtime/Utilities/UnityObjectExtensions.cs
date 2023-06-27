/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.WitAi
{
    public static class UnityObjectExtensions
    {
        // Safely destroys
        public static void DestroySafely(this Object unityObject)
        {
            // Ignore null
            if (unityObject == null)
            {
                return;
            }

            #if UNITY_EDITOR
            // Editor only destroy
            if (!Application.isPlaying)
            {
                MonoBehaviour.DestroyImmediate(unityObject);
                return;
            }
            #endif

            // Destroy object
            MonoBehaviour.Destroy(unityObject);
        }
    }
}
