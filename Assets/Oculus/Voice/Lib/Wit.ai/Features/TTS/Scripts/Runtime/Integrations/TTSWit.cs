/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Events;
using Meta.WitAi.TTS.Interfaces;
using Meta.WitAi.Requests;
using UnityEngine.Serialization;

namespace Meta.WitAi.TTS.Integrations
{
    [Serializable]
    public class TTSWitVoiceSettings : TTSVoiceSettings
    {
        // Default values
        public const string DEFAULT_VOICE = "Charlie";
        public const string DEFAULT_STYLE = "default";

        /// <summary>
        /// Unique voice name
        /// </summary>
        public string voice = DEFAULT_VOICE;
        /// <summary>
        /// Voice style (ex. formal, fast)
        /// </summary>
        public string style = DEFAULT_STYLE;
        [Range(50, 200)]
        public int speed = 100;
        [Range(25, 200)]
        public int pitch = 100;
    }
    [Serializable]
    public struct TTSWitRequestSettings
    {
        public WitConfiguration configuration;
        public TTSWitAudioType audioType;
        public bool audioStream;
        [Tooltip("Amount of clip length in seconds that must be received before stream is considered ready.")]
        public float audioStreamReadyDuration;
        [Tooltip("Total samples to be used to generate clip. A new clip will be generated every time this chunk size is surpassed.")]
        public float audioStreamChunkLength;
        [Tooltip("Amount of placeholder stream clips to be generated on service generation.")]
        public int audioStreamPreloadCount;
    }

    public class TTSWit : TTSService, ITTSVoiceProvider, ITTSWebHandler, IWitConfigurationProvider
    {
        #region TTSService
        // Voice provider
        public override ITTSVoiceProvider VoiceProvider => this;
        // Request handler
        public override ITTSWebHandler WebHandler => this;
        // Runtime cache handler
        public override ITTSRuntimeCacheHandler RuntimeCacheHandler
        {
            get
            {
                if (_runtimeCache == null)
                {
                    _runtimeCache = gameObject.GetComponent<ITTSRuntimeCacheHandler>();
                }
                return _runtimeCache;
            }
        }
        private ITTSRuntimeCacheHandler _runtimeCache;
        // Cache handler
        public override ITTSDiskCacheHandler DiskCacheHandler
        {
            get
            {
                if (_diskCache == null)
                {
                    _diskCache = gameObject.GetComponent<ITTSDiskCacheHandler>();
                }
                return _diskCache;
            }
        }
        private ITTSDiskCacheHandler _diskCache;

        // Configuration provider
        public WitConfiguration Configuration => RequestSettings.configuration;

        // Use wit tts vrequest type
        protected override AudioType GetAudioType()
        {
            return WitTTSVRequest.GetAudioType(RequestSettings.audioType);
        }
        // Preload stream cache
        protected override void Awake()
        {
            base.Awake();
            PreloadStreamCache();
        }
        // Add delegates
        protected override void OnEnable()
        {
            base.OnEnable();
            AudioStreamHandler.OnClipUpdated += OnStreamClipUpdated;
            AudioStreamHandler.OnStreamComplete += OnStreamClipComplete;
        }
        // Remove delegates
        protected override void OnDisable()
        {
            base.OnDisable();
            AudioStreamHandler.OnClipUpdated -= OnStreamClipUpdated;
            AudioStreamHandler.OnStreamComplete -= OnStreamClipComplete;
        }
        // Destroy stream cache
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnloadStreamCache();
        }

        // Clip stream updated
        private void OnStreamClipUpdated(AudioClip oldClip, AudioClip newClip)
        {
            TTSClipData[] clips = GetAllRuntimeCachedClips();
            if (clips == null)
            {
                return;
            }
            foreach (var clipData in clips)
            {
                if (oldClip == clipData.clip)
                {
                    clipData.clip = newClip;
                    WebStreamEvents?.OnStreamClipUpdate?.Invoke(clipData);
                }
            }
        }
        // Clip stream complete
        private void OnStreamClipComplete(AudioClip clip)
        {
            TTSClipData[] clips = GetAllRuntimeCachedClips();
            if (clips == null)
            {
                return;
            }
            foreach (var clipData in clips)
            {
                if (clip == clipData.clip)
                {
                    WebStreamEvents?.OnStreamComplete?.Invoke(clipData);
                }
            }
        }
        #endregion

        #region AudioStream Cache
        // Simple check for cache
        private bool _wasCached = false;
        // Preload the stream cache
        private void PreloadStreamCache()
        {
            // Ignore
            if (!RequestSettings.audioStream || RequestSettings.audioStreamPreloadCount <= 0 || _wasCached)
            {
                return;
            }

            // Total samples to preload
            int totalSamples = Mathf.CeilToInt(RequestSettings.audioStreamChunkLength *
                                               WitConstants.ENDPOINT_TTS_CHANNELS *
                                               WitConstants.ENDPOINT_TTS_SAMPLE_RATE);

            // Preload specified amount of clips
            _wasCached = true;
            AudioStreamHandler.PreloadCachedClips(RequestSettings.audioStreamPreloadCount, totalSamples, WitConstants.ENDPOINT_TTS_CHANNELS, WitConstants.ENDPOINT_TTS_SAMPLE_RATE);
        }
        // Preload the stream cache
        private void UnloadStreamCache()
        {
            // Ignore if was not cached
            if (!_wasCached)
            {
                return;
            }

            // Destroy all cached clips
            AudioStreamHandler.DestroyCachedClips();
            _wasCached = false;
        }
        #endregion AudioStream Cache

        #region ITTSWebHandler Streams
        // Request settings
        [Header("Web Request Settings")]
        [FormerlySerializedAs("_settings")]
        public TTSWitRequestSettings RequestSettings = new TTSWitRequestSettings
        {
            audioType = TTSWitAudioType.PCM,
            audioStream = true,
            audioStreamReadyDuration = 0.1f, // .1 seconds received before starting playback
            audioStreamChunkLength = 5f, // 5 seconds per clip generation
            audioStreamPreloadCount = 3 // 3 clips preloaded to be streamed at once
        };

        // Use settings web stream events
        public TTSStreamEvents WebStreamEvents { get; set; } = new TTSStreamEvents();

        // Requests bly clip id
        private Dictionary<string, VRequest> _webStreams = new Dictionary<string, VRequest>();

        // Whether TTSService is valid
        public override string GetInvalidError()
        {
            string invalidError = base.GetInvalidError();
            if (!string.IsNullOrEmpty(invalidError))
            {
                return invalidError;
            }
            if (RequestSettings.configuration == null)
            {
                return "No WitConfiguration Set";
            }
            if (string.IsNullOrEmpty(RequestSettings.configuration.GetClientAccessToken()))
            {
                return "No WitConfiguration Client Token";
            }
            return string.Empty;
        }
        // Ensures text can be sent to wit web service
        public string IsTextValid(string textToSpeak) => string.IsNullOrEmpty(textToSpeak) ? WitConstants.ENDPOINT_TTS_NO_TEXT : string.Empty;

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="onStreamSetupComplete">Stream setup complete: returns clip and error if applicable</param>
        public void RequestStreamFromWeb(TTSClipData clipData)
        {
            // Stream begin
            WebStreamEvents?.OnStreamBegin?.Invoke(clipData);

            // Check if valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebStreamEvents?.OnStreamError?.Invoke(clipData, validError);
                return;
            }
            // Ignore if already performing
            if (_webStreams.ContainsKey(clipData.clipID))
            {
                CancelWebStream(clipData);
            }

            // Whether to stream
            bool stream = Application.isPlaying && RequestSettings.audioStream;

            // Request tts
            WitTTSVRequest request = new WitTTSVRequest(RequestSettings.configuration);
            request.RequestStream(clipData.textToSpeak, RequestSettings.audioType, stream, RequestSettings.audioStreamReadyDuration, RequestSettings.audioStreamChunkLength, clipData.queryParameters,
                (clip, error) =>
                {
                    // Apply
                    _webStreams.Remove(clipData.clipID);
                    clipData.clip = clip;
                    // Unloaded
                    if (clipData.loadState == TTSClipLoadState.Unloaded)
                    {
                        error = WitConstants.CANCEL_ERROR;
                        clip.DestroySafely();
                        clip = null;
                    }
                    // Error
                    if (!string.IsNullOrEmpty(error))
                    {
                        if (string.Equals(error, WitConstants.CANCEL_ERROR, StringComparison.CurrentCultureIgnoreCase))
                        {
                            WebStreamEvents?.OnStreamCancel?.Invoke(clipData);
                        }
                        else
                        {
                            WebStreamEvents?.OnStreamError?.Invoke(clipData, error);
                        }
                    }
                    // Success
                    else
                    {
                        clipData.clip.name = clipData.clipID;
                        WebStreamEvents?.OnStreamReady?.Invoke(clipData);
                        if (!stream)
                        {
                            WebStreamEvents?.OnStreamComplete?.Invoke(clipData);
                        }
                    }
                },
                (progress) => clipData.loadProgress = progress);
            _webStreams[clipData.clipID] = request;
        }
        /// <summary>
        /// Cancel web stream
        /// </summary>
        /// <param name="clipID">Unique clip id</param>
        public bool CancelWebStream(TTSClipData clipData)
        {
            // Ignore without
            if (!_webStreams.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            VRequest request = _webStreams[clipData.clipID];
            _webStreams.Remove(clipData.clipID);

            // Destroy immediately
            request?.Cancel();
            request = null;

            // Call delegate
            WebStreamEvents?.OnStreamCancel?.Invoke(clipData);

            // Success
            return true;
        }
        #endregion

        #region ITTSWebHandler Downloads
        // Use settings web download events
        public TTSDownloadEvents WebDownloadEvents { get; set; } = new TTSDownloadEvents();

        // Requests by clip id
        private Dictionary<string, WitVRequest> _webDownloads = new Dictionary<string, WitVRequest>();

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="downloadPath">Path to save clip</param>
        public void RequestDownloadFromWeb(TTSClipData clipData, string downloadPath)
        {
            // Begin
            WebDownloadEvents?.OnDownloadBegin?.Invoke(clipData, downloadPath);

            // Ensure valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, validError);
                return;
            }
            // Abort if already performing
            if (_webDownloads.ContainsKey(clipData.clipID))
            {
                CancelWebDownload(clipData, downloadPath);
            }

            // Request tts
            WitTTSVRequest request = new WitTTSVRequest(RequestSettings.configuration);
            request.RequestDownload(downloadPath, clipData.textToSpeak, RequestSettings.audioType, clipData.queryParameters,
                (success, error) =>
                {
                    _webDownloads.Remove(clipData.clipID);
                    if (string.IsNullOrEmpty(error))
                    {
                        WebDownloadEvents?.OnDownloadSuccess?.Invoke(clipData, downloadPath);
                    }
                    else
                    {
                        WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, error);
                    }
                },
                (progress) => clipData.loadProgress = progress);
            _webDownloads[clipData.clipID] = request;
        }
        /// <summary>
        /// Method for cancelling a running load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        public bool CancelWebDownload(TTSClipData clipData, string downloadPath)
        {
            // Ignore if not performing
            if (!_webDownloads.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            WitVRequest request = _webDownloads[clipData.clipID];
            _webDownloads.Remove(clipData.clipID);

            // Destroy immediately
            request?.Cancel();
            request = null;

            // Download cancelled
            WebDownloadEvents?.OnDownloadCancel?.Invoke(clipData, downloadPath);

            // Success
            return true;
        }
        #endregion

        #region ITTSVoiceProvider
        // Preset voice settings
        [Header("Voice Settings")]
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        [SerializeField] private TTSWitVoiceSettings[] _presetVoiceSettings;
        public TTSWitVoiceSettings[] PresetWitVoiceSettings => _presetVoiceSettings;

        // Cast to voice array
        public TTSVoiceSettings[] PresetVoiceSettings
        {
            get
            {
                if (_presetVoiceSettings == null || _presetVoiceSettings.Length == 0)
                {
                    _presetVoiceSettings = new TTSWitVoiceSettings[] { new TTSWitVoiceSettings() };
                }
                return _presetVoiceSettings;
            }
        }
        // Default voice setting uses the first voice in the list
        public TTSVoiceSettings VoiceDefaultSettings => PresetVoiceSettings[0];

        #if UNITY_EDITOR
        // Apply settings
        public void SetVoiceSettings(TTSWitVoiceSettings[] newVoiceSettings)
        {
            _presetVoiceSettings = newVoiceSettings;
        }
        #endif

        // Convert voice settings into dictionary to be used with web requests
        private const string SETTINGS_KEY = "settingsID";
        private const string VOICE_KEY = "voice";
        private const string STYLE_KEY = "style";
        public Dictionary<string, string> EncodeVoiceSettings(TTSVoiceSettings voiceSettings)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (voiceSettings != null)
            {
                foreach (FieldInfo field in voiceSettings.GetType().GetFields())
                {
                    if (!field.IsStatic && !string.Equals(field.Name, SETTINGS_KEY, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Get field value
                        object fieldVal = field.GetValue(voiceSettings);

                        // Clamp in between range
                        RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();
                        if (range != null && field.FieldType == typeof(int))
                        {
                            int oldFloat = (int) fieldVal;
                            int newFloat = Mathf.Clamp(oldFloat, (int)range.min, (int)range.max);
                            if (oldFloat != newFloat)
                            {
                                fieldVal = newFloat;
                            }
                        }

                        // Apply
                        parameters[field.Name] = fieldVal.ToString();
                    }
                }

                // Set default if no voice is provided
                if (!parameters.ContainsKey(VOICE_KEY) || string.IsNullOrEmpty(parameters[VOICE_KEY]))
                {
                    parameters[VOICE_KEY] = TTSWitVoiceSettings.DEFAULT_VOICE;
                }
                // Set default if no style is given
                if (!parameters.ContainsKey(STYLE_KEY) || string.IsNullOrEmpty(parameters[STYLE_KEY]))
                {
                    parameters[STYLE_KEY] = TTSWitVoiceSettings.DEFAULT_STYLE;
                }
            }
            return parameters;
        }
        // Returns an error if request is not valid
        private string IsRequestValid(TTSClipData clipData, WitConfiguration configuration)
        {
            // Invalid tts
            string invalidError = GetInvalidError();
            if (!string.IsNullOrEmpty(invalidError))
            {
                return invalidError;
            }
            // Invalid clip
            if (clipData == null)
            {
                return "No clip data provided";
            }
            // Success
            return string.Empty;
        }
        #endregion
    }
}
