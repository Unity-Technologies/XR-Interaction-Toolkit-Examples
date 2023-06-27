/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.WitAi.TTS.Data
{
    // TTS Cache disk location
    public enum TTSDiskCacheLocation
    {
        /// <summary>
        /// Does not cache
        /// </summary>
        Stream,
        /// <summary>
        /// Stores files in editor only & loads files from internal project location (Application.streamingAssetsPath)
        /// </summary>
        Preload,
        /// <summary>
        /// Stores files at persistent location (Application.persistentDataPath)
        /// </summary>
        Persistent,
        /// <summary>
        /// Stores files at temporary cache location (Application.temporaryCachePath)
        /// </summary>
        Temporary
    }

    [Serializable]
    public class TTSDiskCacheSettings
    {
        /// <summary>
        /// Where the TTS clip should be cached
        /// </summary>
        public TTSDiskCacheLocation DiskCacheLocation = TTSDiskCacheLocation.Stream;

        /// <summary>
        /// Where the TTS clip should streamed from cache
        /// </summary>
        public bool StreamFromDisk = false;
        /// <summary>
        /// Length of a streamed clip buffer in seconds
        /// </summary>
        public float StreamBufferLength = 5f;
    }
}
