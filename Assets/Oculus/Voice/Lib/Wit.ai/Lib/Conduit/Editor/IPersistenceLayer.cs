/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Mediates storage for small data that need to be persisted across projects.
    /// </summary>
    internal interface IPersistenceLayer
    {
        bool HasKey(string key);
        void SetString(string key, string value);
        string GetString(string key);
        void SetInt(string key, int value);
        int GetInt(string key);
    }
}
