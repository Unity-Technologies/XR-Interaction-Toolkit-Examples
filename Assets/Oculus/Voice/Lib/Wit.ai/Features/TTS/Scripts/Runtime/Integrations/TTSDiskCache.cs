/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Events;
using Meta.WitAi.TTS.Interfaces;
using Meta.WitAi.Utilities;
using Meta.WitAi.Requests;

namespace Meta.WitAi.TTS.Integrations
{
    public class TTSDiskCache : MonoBehaviour, ITTSDiskCacheHandler
    {
        [Header("Disk Cache Settings")]
        /// <summary>
        /// The relative path from the DiskCacheLocation in TTSDiskCacheSettings
        /// </summary>
        [SerializeField] private string _diskPath = "TTS/";
        public string DiskPath => _diskPath;

        /// <summary>
        /// The cache default settings
        /// </summary>
        [SerializeField] private TTSDiskCacheSettings _defaultSettings = new TTSDiskCacheSettings();
        public TTSDiskCacheSettings DiskCacheDefaultSettings => _defaultSettings;

        /// <summary>
        /// The cache streaming events
        /// </summary>
        [SerializeField] private TTSStreamEvents _events = new TTSStreamEvents();
        public TTSStreamEvents DiskStreamEvents
        {
            get => _events;
            set { _events = value; }
        }

        // All currently performing stream requests
        private Dictionary<string, VRequest> _streamRequests = new Dictionary<string, VRequest>();

        // Cancel all requests
        protected virtual void OnDestroy()
        {
            Dictionary<string, VRequest> requests = _streamRequests;
            _streamRequests.Clear();
            foreach (var request in requests.Values)
            {
                request.Cancel();
            }
        }

        /// <summary>
        /// Builds full cache path
        /// </summary>
        /// <param name="clipData"></param>
        /// <returns></returns>
        public string GetDiskCachePath(TTSClipData clipData)
        {
            // Disabled
            if (!ShouldCacheToDisk(clipData))
            {
                return string.Empty;
            }

            // Get directory path
            TTSDiskCacheLocation location = clipData.diskCacheSettings.DiskCacheLocation;
            string directory = string.Empty;
            switch (location)
            {
                case TTSDiskCacheLocation.Persistent:
                    directory = Application.persistentDataPath;
                    break;
                case TTSDiskCacheLocation.Temporary:
                    directory = Application.temporaryCachePath;
                    break;
                case TTSDiskCacheLocation.Preload:
                    directory = Application.streamingAssetsPath;
                    break;
            }
            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            // Add tts cache path & clean
            directory = Path.Combine(directory, DiskPath);

            // Generate tts directory if possible
            if (location != TTSDiskCacheLocation.Preload || !Application.isPlaying)
            {
                if (!IOUtility.CreateDirectory(directory, true))
                {
                    VLog.E($"Failed to create tts directory\nPath: {directory}\nLocation: {location}");
                    return string.Empty;
                }
            }

            // Return clip path
            return Path.Combine(directory, clipData.clipID + "." + WitTTSVRequest.GetAudioExtension(clipData.audioType));
        }

        /// <summary>
        /// Determine if should cache to disk or not
        /// </summary>
        /// <param name="clipData">All clip data</param>
        /// <returns>Returns true if should cache to disk</returns>
        public bool ShouldCacheToDisk(TTSClipData clipData)
        {
            return clipData != null && clipData.diskCacheSettings.DiskCacheLocation != TTSDiskCacheLocation.Stream && !string.IsNullOrEmpty(clipData.clipID);
        }

        /// <summary>
        /// Determines if file is cached on disk
        /// </summary>
        /// <param name="clipData">Request data</param>
        /// <returns>True if file is on disk</returns>
        public void CheckCachedToDisk(TTSClipData clipData, Action<TTSClipData, bool> onCheckComplete)
        {
            // Get path
            string cachePath = GetDiskCachePath(clipData);
            if (string.IsNullOrEmpty(cachePath))
            {
                onCheckComplete?.Invoke(clipData, false);
                return;
            }

            // Check if file exists
            VRequest request = new VRequest();
            bool canPerform = request.RequestFileExists(cachePath, (success, error) =>
            {
                // Remove
                if (_streamRequests.ContainsKey(clipData.clipID))
                {
                    _streamRequests.Remove(clipData.clipID);
                }
                // Complete
                onCheckComplete(clipData, success);
            });
            if (canPerform)
            {
                _streamRequests[clipData.clipID] = request;
            }
        }

        /// <summary>
        /// Performs async load request
        /// </summary>
        public void StreamFromDiskCache(TTSClipData clipData)
        {
            // Invoke begin
            DiskStreamEvents?.OnStreamBegin?.Invoke(clipData);

            // Get file path
            string filePath = GetDiskCachePath(clipData);

            // Load clip async
            VRequest request = new VRequest();
            bool canPerform = request.RequestAudioClip(new Uri(request.CleanUrl(filePath)), (clip, error) =>
            {
                // Apply clip
                clipData.clip = clip;
                // Call on complete
                OnStreamComplete(clipData, error);
            }, clipData.audioType, clipData.diskCacheSettings.StreamFromDisk, 0.01f, clipData.diskCacheSettings.StreamBufferLength, (progress) => clipData.loadProgress = progress);
            if (canPerform)
            {
                _streamRequests[clipData.clipID] = request;
            }
        }
        /// <summary>
        /// Cancels unity request
        /// </summary>
        public void CancelDiskCacheStream(TTSClipData clipData)
        {
            // Ignore if not currently streaming
            if (!_streamRequests.ContainsKey(clipData.clipID))
            {
                return;
            }

            // Get request
            VRequest request = _streamRequests[clipData.clipID];
            _streamRequests.Remove(clipData.clipID);

            // Cancel immediately
            request?.Cancel();
            request = null;

            // Call cancel
            DiskStreamEvents?.OnStreamCancel?.Invoke(clipData);
        }
        // On stream completion
        protected virtual void OnStreamComplete(TTSClipData clipData, string error)
        {
            // Ignore if not currently streaming
            if (!_streamRequests.ContainsKey(clipData.clipID))
            {
                return;
            }

            // Remove from list
            _streamRequests.Remove(clipData.clipID);

            // Error
            if (!string.IsNullOrEmpty(error))
            {
                DiskStreamEvents?.OnStreamError?.Invoke(clipData, error);
            }
            // Success
            else
            {
                DiskStreamEvents?.OnStreamReady?.Invoke(clipData);
            }
        }
    }
}
