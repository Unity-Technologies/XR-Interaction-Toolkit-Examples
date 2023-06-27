/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

// Uncomment when added to Wit.ai
//#define OGG_SUPPORT

using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    // Supported audio types
    public enum TTSWitAudioType
    {
        PCM = 0,
        MPEG = 1,
        #if OGG_SUPPORT
        OGG = 3,
        #endif
        WAV = 2
    }
    public class WitTTSVRequest : WitVRequest
    {
        // Constructor
        public WitTTSVRequest(IWitRequestConfiguration configuration) : base(configuration, null, false)
        {
            Timeout = WitConstants.ENDPOINT_TTS_TIMEOUT;
        }

        // Cast audio type
        public static AudioType GetAudioType(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                #if OGG_SUPPORT
                case TTSWitAudioType.OGG:
                    return AudioType.OGGVORBIS;
                #endif
                case TTSWitAudioType.MPEG:
                    return AudioType.MPEG;
                case TTSWitAudioType.WAV:
                    return AudioType.WAV;
                // Custom implementation
                case TTSWitAudioType.PCM:
                default:
                    return AudioType.UNKNOWN;
            }
        }
        // Get audio type
        public static string GetAudioMimeType(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                // PCM
                case TTSWitAudioType.PCM:
                    return "audio/raw";
                #if OGG_SUPPORT
                // OGG
                case TTSWitAudioType.OGG:
                #endif
                // MP3 & WAV
                case TTSWitAudioType.MPEG:
                case TTSWitAudioType.WAV:
                default:
                    return $"audio/{witAudioType.ToString().ToLower()}";
            }
        }
        // Get audio extension
        public static string GetAudioExtension(TTSWitAudioType witAudioType) => GetAudioExtension(GetAudioType(witAudioType));
        // Get audio extension
        public static string GetAudioExtension(AudioType audioType)
        {
            switch (audioType)
            {
                // PCM
                case AudioType.UNKNOWN:
                    return "raw";
                // OGG
                case AudioType.OGGVORBIS:
                    return "ogg";
                // MP3
                case AudioType.MPEG:
                    return "mp3";
                // WAV
                case AudioType.WAV:
                    return "wav";
                default:
                    VLog.W($"Attempting to process unsupported audio type: {audioType}");
                    return audioType.ToString().ToLower();
            }
        }
        // Whether streamed audio is allowed by unity
        public static bool CanStreamAudio(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                // Raw PCM: Supported by Wit.ai & custom unity implementation (DownloadHandlerRawPCM)
                case TTSWitAudioType.PCM:
                    return true;
                #if OGG_SUPPORT
                // OGG: Supported by Unity (DownloadHandlerAudioClip) but not by Wit.ai
                case TTSWitAudioType.OGG:
                    return true;
                #endif
                // MP3: Supported by Wit.ai but not by Unity (DownloadHandlerAudioClip)
                case TTSWitAudioType.MPEG:
                    return false;
                // WAV: does not support streaming
                case TTSWitAudioType.WAV:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Streams text to speech audio clip
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken</param>
        /// <param name="ttsData">Info on tts voice settings</param>
        /// <param name="onClipReady">Clip ready to be played</param>
        /// <param name="onProgress">Clip load progress</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestStream(string textToSpeak,
            TTSWitAudioType audioType,
            bool audioStream,
            float audioStreamReadyDuration, float audioStreamChunkLength,
            Dictionary<string, string> ttsData,
            RequestCompleteDelegate<AudioClip> onClipReady,
            RequestProgressDelegate onProgress = null)
        {
            // Error if no text is provided
            if (string.IsNullOrEmpty(textToSpeak))
            {
                onClipReady?.Invoke(null, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }
            // Warn if incompatible with streaming
            if (audioStream && !CanStreamAudio(audioType))
            {
                VLog.W($"Wit cannot stream {audioType} files please use {TTSWitAudioType.PCM} instead.");
            }

            // Async encode
            EncodePostBytesAsync(textToSpeak, ttsData, (bytes) =>
            {
                // Get tts unity request
                UnityWebRequest unityRequest = GetUnityRequest(audioType, bytes);

                // Perform an audio stream request
                RequestAudioClip(unityRequest, onClipReady, GetAudioType(audioType), audioStream,
                    audioStreamReadyDuration, audioStreamChunkLength, onProgress);
            });
            return true;
        }

        /// <summary>
        /// TTS streaming audio request
        /// </summary>
        /// <param name="downloadPath">Download path</param>
        /// <param name="textToSpeak">Text to be spoken</param>
        /// <param name="ttsData">Info on tts voice settings</param>
        /// <param name="onComplete">Clip completed download</param>
        /// <param name="onProgress">Clip load progress</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestDownload(string downloadPath,
            string textToSpeak,
            TTSWitAudioType audioType,
            Dictionary<string, string> ttsData,
            RequestCompleteDelegate<bool> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Error
            if (string.IsNullOrEmpty(textToSpeak))
            {
                onComplete?.Invoke(false, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }

            // Async encode
            EncodePostBytesAsync(textToSpeak, ttsData, (bytes) =>
            {
                // Get tts unity request
                UnityWebRequest unityRequest = GetUnityRequest(audioType, bytes);

                // Perform an audio stream request
                RequestFileDownload(downloadPath, unityRequest, onComplete, onProgress);
            });
            return true;
        }

        // Encode post bytes async
        private void EncodePostBytesAsync(string textToSpeak, Dictionary<string, string> ttsData,
            Action<byte[]> onEncoded) => ThreadUtility.PerformInBackground(() => EncodePostData(textToSpeak, ttsData),
            (bytes, error) => onEncoded(bytes));

        // Encode tts post bytes
        private byte[] EncodePostData(string textToSpeak, Dictionary<string, string> ttsData)
        {
            ttsData[WitConstants.ENDPOINT_TTS_PARAM] = textToSpeak;
            string jsonString = JsonConvert.SerializeObject(ttsData);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        // Internal base method for tts request
        private UnityWebRequest GetUnityRequest(TTSWitAudioType audioType, byte[] postData)
        {
            // Get uri
            Uri uri = GetUri(Configuration.GetEndpointInfo().Synthesize);

            // Generate request
            UnityWebRequest unityRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            unityRequest.SetRequestHeader(WitConstants.HEADER_POST_CONTENT, "application/json");
            unityRequest.SetRequestHeader(WitConstants.HEADER_GET_CONTENT, GetAudioMimeType(audioType));

            // Add upload handler
            unityRequest.uploadHandler = new UploadHandlerRaw(postData);

            // Perform json request
            return unityRequest;
        }
    }
}
