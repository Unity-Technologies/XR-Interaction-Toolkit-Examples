/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;

namespace Meta.Conduit.Editor
{
    /// <inheritdoc/>
    internal class PersistenceLayer : IPersistenceLayer
    {
        public bool HasKey(string key)
        {
            return EditorPrefs.HasKey(key);
        }

        public void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(key, value);
        }

        public void SetString(string key, string value)
        {
            EditorPrefs.SetString(key, value);
        }

        public string GetString(string key)
        {
            return EditorPrefs.GetString(key);
        }

        public int GetInt(string key)
        {
            return EditorPrefs.GetInt(key);
        }
    }
}
