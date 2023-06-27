/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if UNITY_ANDROID && UNITY_EDITOR
#define FAKE_JAR_LOAD
#endif

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Meta.WitAi.Json;
using Unity.Collections;

namespace Meta.WitAi.Requests
{
    // VRequest streamable interface
    public interface IVRequestStreamable
    {
        bool IsStreamReady { get; }
        bool IsStreamComplete { get; }
    }

    /// <summary>
    /// Class for performing web requests using UnityWebRequest
    /// </summary>
    public class VRequest
    {
        /// <summary>
        /// Will only start new requests if there are less than this number
        /// If <= 0, then all requests will run immediately
        /// </summary>
        public static int MaxConcurrentRequests = 3;
        // Currently transmitting requests
        private static int _requestCount = 0;

        // Request progress delegate
        public delegate void RequestProgressDelegate(float progress);
        // Default request completion delegate
        public delegate void RequestCompleteDelegate<TResult>(TResult result, string error);

        #region INSTANCE
        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public int Timeout { get; set; } = 5;

        /// <summary>
        /// If request is currently being performed
        /// </summary>
        public bool IsPerforming { get; private set; } = false;

        /// <summary>
        /// Whether or not the completion delegate has been called
        /// </summary>
        public bool IsComplete { get; private set; } = false;
        /// <summary>
        /// Response Code if applicable
        /// </summary>
        public int ResponseCode { get; set; } = 0;

        /// <summary>
        /// Current progress for get requests
        /// </summary>
        public float UploadProgress { get; private set; } = 0f;
        /// <summary>
        /// Current progress for download
        /// </summary>
        public float DownloadProgress { get; private set; } = 0f;

        // Actual request
        private UnityWebRequest _request;
        // Callbacks for progress & completion
        private RequestProgressDelegate _onDownloadProgress;
        private RequestCompleteDelegate<UnityWebRequest> _onComplete;

        // Coroutine running the request
        private CoroutineUtility.CoroutinePerformer _coroutine;

        /// <summary>
        /// Initialize with a request and an on completion callback
        /// </summary>
        /// <param name="unityRequest">The unity request to be performed</param>
        /// <param name="onProgress">The callback on get progress</param>
        /// <param name="onComplete">The callback on completion, returns the request & error string</param>
        /// <returns>False if the request cannot be performed</returns>
        public virtual bool Request(UnityWebRequest unityRequest, RequestCompleteDelegate<UnityWebRequest> onComplete, RequestProgressDelegate onDownloadProgress = null)
        {
            // Already setup
            if (_request != null)
            {
                onComplete?.Invoke(unityRequest, "Request is already being performed");
                return false;
            }

            // Setup
            _request = unityRequest;
            _onDownloadProgress = onDownloadProgress;
            _onComplete = onComplete;
            IsPerforming = false;
            IsComplete = false;
            UploadProgress = 0f;
            DownloadProgress = 0f;

            // Add all headers
            Dictionary<string, string> headers = GetHeaders();
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    _request.SetRequestHeader(key, headers[key]);
                }
            }

            // Use request's timeout value
            _request.timeout = Timeout;

            // Dispose handlers automatically
            _request.disposeUploadHandlerOnDispose = true;
            _request.disposeDownloadHandlerOnDispose = true;

            // Begin
            _coroutine = CoroutineUtility.StartCoroutine(PerformUpdate());

            // Success
            return true;
        }
        /// <summary>
        /// Clean the url prior to use
        /// </summary>
        public virtual string CleanUrl(string url)
        {
            // Get url
            string result = url;
            // Add file:// if needed
            if (!Regex.IsMatch(result, "(http:|https:|file:|jar:).*"))
            {
                result = $"file://{result}";
            }
            // Return url
            return result;
        }
        // Override for custom headers
        protected virtual Dictionary<string, string> GetHeaders() => null;
        // Perform update
        protected virtual IEnumerator PerformUpdate()
        {
            // Continue while request exists & is not complete
            while (!IsRequestComplete())
            {
                // Wait
                yield return null;

                // Waiting to begin
                if (!IsPerforming)
                {
                    // Can start
                    if (MaxConcurrentRequests <= 0 || _requestCount < MaxConcurrentRequests)
                    {
                        _requestCount++;
                        Begin();
                    }
                }
                // Update progresses
                else
                {
                    // Set upload progress
                    float newProgress = _request.uploadProgress;
                    if (!UploadProgress.Equals(newProgress))
                    {
                        UploadProgress = newProgress;
                    }

                    // Set download progress
                    newProgress = _request.downloadProgress;
                    if (!DownloadProgress.Equals(newProgress))
                    {
                        DownloadProgress = newProgress;
                        _onDownloadProgress?.Invoke(DownloadProgress);
                    }

                    // Stream is ready
                    if (_onComplete != null && _request.downloadHandler is IVRequestStreamable streamHandler)
                    {
                        if (streamHandler.IsStreamReady)
                        {
                            _onComplete.Invoke(_request, string.Empty);
                            _onComplete = null;
                        }
                    }
                }
            }
            // Complete
            Complete();
        }
        // Begin request
        protected virtual void Begin()
        {
            IsPerforming = true;
            UploadProgress = 0f;
            DownloadProgress = 0f;
            _onDownloadProgress?.Invoke(DownloadProgress);
            _request.SendWebRequest();
        }
        // Check for whether request is complete
        protected virtual bool IsRequestComplete()
        {
            // No request
            if (_request == null)
            {
                return true;
            }
            // Request still in progress
            if (!_request.isDone)
            {
                return false;
            }
            // No error & download handler
            if (string.IsNullOrEmpty(_request.error) && _request.downloadHandler != null)
            {
                // Stream is still finishing
                if (_request.downloadHandler is IVRequestStreamable streamHandler)
                {
                    if (!streamHandler.IsStreamComplete)
                    {
                        return false;
                    }
                }
                // Download handler not complete
                else if (!_request.downloadHandler.isDone)
                {
                    return false;
                }
            }
            // Complete
            return true;
        }
        // Request complete
        protected virtual void Complete()
        {
            // Perform callback
            if (IsPerforming && IsRequestComplete())
            {
                DownloadProgress = 1f;
                ResponseCode = (int)_request.responseCode;
                _onDownloadProgress?.Invoke(DownloadProgress);
                _onComplete?.Invoke(_request, GetSpecificRequestError(_request));
            }

            // Unload
            Unload();
        }
        // Abort request
        public virtual void Cancel()
        {
            // Cancel
            if (_onComplete != null && _request != null)
            {
                DownloadProgress = 1f;
                _onDownloadProgress?.Invoke(DownloadProgress);
                _onComplete?.Invoke(_request, WitConstants.CANCEL_ERROR);
            }

            // Unload
            Unload();
        }
        // Request destroy
        protected virtual void Unload()
        {
            // Cancel coroutine
            if (_coroutine != null)
            {
                _coroutine.CoroutineCancel();
                _coroutine = null;
            }

            // Complete
            if (IsPerforming)
            {
                IsPerforming = false;
                _requestCount--;
            }

            // Remove delegates
            _onDownloadProgress = null;
            _onComplete = null;

            // Dispose
            if (_request != null)
            {
                // Additional cleanup
                if (_request.downloadHandler is AudioStreamHandler audioStreamer)
                {
                    audioStreamer.CleanUp();
                }
                // Dispose handlers
                _request.uploadHandler?.Dispose();
                _request.downloadHandler?.Dispose();
                // Dispose request
                _request.Dispose();
                _request = null;
            }

            // Officially complete
            IsComplete = true;
        }
        // Returns more specific request error
        public static string GetSpecificRequestError(UnityWebRequest request)
        {
            // Get error & return if empty
            string error = request.error;
            string result = error;
            if (string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Ignore without download handler
            if (request.downloadHandler == null)
            {
                return result;
            }

            // Ignore without downloaded json
            string downloadedJson = string.Empty;
            try
            {
                byte[] downloadedBytes = request.downloadHandler.data;
                downloadedJson = Encoding.UTF8.GetString(downloadedBytes);
            }
            catch (Exception e)
            {
                VLog.W($"VRequest failed to parse downloaded text\n{e}");
            }
            if (string.IsNullOrEmpty(downloadedJson))
            {
                return result;
            }

            // Set error to json
            result = $"{error}\nServer Response: {downloadedJson}";

            // Decode
            WitResponseNode downloadedNode = WitResponseNode.Parse(downloadedJson);
            if (downloadedNode == null)
            {
                return result;
            }

            // Check for error
            WitResponseClass downloadedClass = downloadedNode.AsObject;
            if (!downloadedClass.HasChild(WitConstants.ENDPOINT_ERROR_PARAM))
            {
                return result;
            }

            // Get final result
            return $"{request.error}\nServer Response Message: {downloadedClass[WitConstants.ENDPOINT_ERROR_PARAM].Value}";
        }
        #endregion

        #region FILE
        /// <summary>
        /// Performs a simple http header request
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="onComplete">Called once header lookup has completed</param>
        /// <returns></returns>
        public bool RequestFileHeaders(Uri uri,
            RequestCompleteDelegate<Dictionary<string, string>> onComplete)
        {
            // Header unity request
            UnityWebRequest unityRequest = UnityWebRequest.Head(uri);

            // Perform request
            return Request(unityRequest, (response, error) =>
            {
                // Error
                if (!string.IsNullOrEmpty(error))
                {
                    onComplete?.Invoke(null, error);
                    return;
                }

                // Headers dictionary if possible
                Dictionary<string, string> headers = response.GetResponseHeaders();
                if (headers == null)
                {
                    onComplete?.Invoke(null, "No headers in response.");
                    return;
                }

                // Success
                onComplete?.Invoke(headers, string.Empty);
            });
        }

        /// <summary>
        /// Performs a simple http header request
        /// </summary>
        /// <param name="uri">Uri to get a file</param>
        /// <param name="onComplete">Called once file data has been loaded</param>
        /// <returns>False if cannot begin request</returns>
        public bool RequestFile(Uri uri,
            RequestCompleteDelegate<byte[]> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Get unity request
            UnityWebRequest unityRequest = UnityWebRequest.Get(uri);
            // Perform request
            return Request(unityRequest, (response, error) =>
            {
                // Error
                if (!string.IsNullOrEmpty(error))
                {
                    onComplete?.Invoke(null, error);
                    return;
                }

                // File data
                byte[] fileData = response?.downloadHandler?.data;
                if (fileData == null)
                {
                    onComplete?.Invoke(null, "No data in response");
                    return;
                }

                // Success
                onComplete?.Invoke(fileData, string.Empty);
            }, onProgress);
        }

        /// <summary>
        /// Download a file using a unityrequest
        /// </summary>
        /// <param name="unityRequest">The unity request to add a download handler to</param>
        /// <param name="onComplete">Called once download has completed</param>
        /// <param name="onProgress">Download progress delegate</param>
        public bool RequestFileDownload(string downloadPath, UnityWebRequest unityRequest,
            RequestCompleteDelegate<bool> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Get temporary path for download
            string tempDownloadPath = downloadPath + ".tmp";
            try
            {
                // Delete temporary file if it already exists
                if (File.Exists(tempDownloadPath))
                {
                    File.Delete(tempDownloadPath);
                }
            }
            catch (Exception e)
            {
                // Failed to delete file
                VLog.W($"Deleting Download File Failed\nPath: {tempDownloadPath}\n\n{e}");
            }

            // Add file download handler
            DownloadHandlerFile fileHandler = new DownloadHandlerFile(tempDownloadPath, true);
            unityRequest.downloadHandler = fileHandler;
            unityRequest.disposeDownloadHandlerOnDispose = true;

            // Perform request
            return Request(unityRequest, (response, error) =>
            {
                try
                {
                    // Handle existing temp file
                    if (File.Exists(tempDownloadPath))
                    {
                        // For error, remove
                        if (!string.IsNullOrEmpty(error))
                        {
                            File.Delete(tempDownloadPath);
                        }
                        // For success, move to final path
                        else
                        {
                            // File already at download path, delete it
                            if (File.Exists(downloadPath))
                            {
                                File.Delete(downloadPath);
                            }

                            // Move to final path
                            File.Move(tempDownloadPath, downloadPath);
                        }
                    }
                }
                catch (Exception e)
                {
                    VLog.W($"Moving Download File Failed\nFrom: {tempDownloadPath}\nTo: {downloadPath}\n\n{e}");
                }

                // Complete
                onComplete?.Invoke(string.IsNullOrEmpty(error), error);
            }, onProgress);
        }

        /// <summary>
        /// Checks if a file exists at a specified location
        /// </summary>
        /// <param name="checkPath">The local file path to be checked</param>
        /// <param name="onComplete">Called once check has completed.  Returns true if file exists</param>
        public bool RequestFileExists(string checkPath,
            RequestCompleteDelegate<bool> onComplete)
        {
            // WebGL & web files, perform a header lookup
            if (checkPath.StartsWith("http"))
            {
                Uri uri = new Uri(CleanUrl(checkPath));
                return RequestFileHeaders(uri, (headers, error) =>
                {
                    onComplete?.Invoke(headers != null, error);
                });
            }

#if FAKE_JAR_LOAD
            // Android editor: simulate jar handling
            if (Application.isPlaying && checkPath.StartsWith(Application.streamingAssetsPath))
#else
            // Jar file (Android streaming assets)
            if (checkPath.StartsWith("jar"))
#endif
            {
                Uri uri = new Uri(CleanUrl(checkPath));
                return RequestFile(uri, (response, error) =>
                {
                    // If getting here, most likely failed but double check anyway
                    onComplete?.Invoke(string.IsNullOrEmpty(error), error);
                },
                (progress) =>
                {
                    // Stop as early as possible
                    if (progress > 0f && progress < 1f)
                    {
                        var localHandle = onComplete;
                        onComplete = null;
                        Cancel();
                        localHandle?.Invoke(true, String.Empty);
                        VLog.D("Async Check File Exists Success");
                    }
                });
            }

            // Can simply use File.IO otherwise
            bool found = File.Exists(checkPath);
            onComplete?.Invoke(found, string.Empty);
            return true;
        }
        #endregion

        #region TEXT
        /// <summary>
        /// Performs a text request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        public bool RequestText(UnityWebRequest unityRequest,
            RequestCompleteDelegate<string> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return Request(unityRequest, (response, error) =>
            {
                // Request error
                string text = response?.downloadHandler?.text;
                if (!string.IsNullOrEmpty(error))
                {
                    onComplete?.Invoke(text, error);
                    return;
                }
                // No text returned
                if (string.IsNullOrEmpty(text))
                {
                    onComplete?.Invoke(string.Empty, "No response contents found");
                    return;
                }
                // Success
                onComplete?.Invoke(text, string.Empty);
            }, onProgress);
        }
        #endregion

        #region JSON
        /// <summary>
        /// Performs a json request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestJson<TData>(UnityWebRequest unityRequest,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Set request header for json
            unityRequest.SetRequestHeader("Content-Type", "application/json");

            // Perform text request
            return RequestText(unityRequest, (text, error) =>
            {
                // Request error
                if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(text))
                {
                    onComplete?.Invoke(default(TData), error);
                    return;
                }

                // Deserialize
                JsonConvert.DeserializeObjectAsync<TData>(text, (result, deserializeSuccess) =>
                {
                    // Return parsed error
                    if (!string.IsNullOrEmpty(error))
                    {
                        onComplete?.Invoke(result, error);
                    }
                    // Parse failed
                    else if (!deserializeSuccess)
                    {
                        onComplete?.Invoke(result, $"Failed to parse json\n{text}");
                    }
                    // Success
                    else
                    {
                        onComplete?.Invoke(result, string.Empty);
                    }
                });
            }, onProgress);
        }

        /// <summary>
        /// Perform a json get request with a specified uri
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        /// <returns></returns>
        public bool RequestJsonGet<TData>(Uri uri,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJson(UnityWebRequest.Get(uri), onComplete, onProgress);
        }

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postData">The data to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestJsonPost<TData>(Uri uri, byte[] postData,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            var unityRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            unityRequest.uploadHandler = new UploadHandlerRaw(postData);
            unityRequest.downloadHandler = new DownloadHandlerBuffer();
            return RequestJson(unityRequest, onComplete, onProgress);
        }
        /// <summary>
        /// Performs a json request by posting a string
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postText">The string to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestJsonPost<TData>(Uri uri, string postText,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJsonPost(uri, Encoding.UTF8.GetBytes(postText), onComplete, onProgress);
        }

        /// <summary>
        /// Performs a json put request with byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="putData">The data to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestJsonPut<TData>(Uri uri, byte[] putData,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            var unityRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPUT);
            unityRequest.uploadHandler = new UploadHandlerRaw(putData);
            unityRequest.downloadHandler = new DownloadHandlerBuffer();
            return RequestJson(unityRequest, onComplete, onProgress);
        }
        /// <summary>
        /// Performs a json put request with a string
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="putText">The string to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        public bool RequestJsonPut<TData>(Uri uri, string putText,
            RequestCompleteDelegate<TData> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            return RequestJsonPut(uri, Encoding.UTF8.GetBytes(putText), onComplete, onProgress);
        }
        #endregion

        #region AUDIO CLIPS
        /// <summary>
        /// Request audio clip with url, type, progress delegate & ready delegate
        /// </summary>
        /// <param name="unityRequest">The unity request to add a download handler to</param>
        /// <param name="onClipReady">Called when the clip is ready for playback or has failed to load</param>
        /// <param name="audioType">The audio type requested (Wav, MP3, etc.)</param>
        /// <param name="audioStream">Whether or not audio should be streamed</param>
        /// <param name="onProgress">Clip progress callback</param>
        public bool RequestAudioClip(UnityWebRequest unityRequest,
            RequestCompleteDelegate<AudioClip> onClipReady,
            AudioType audioType, bool audioStream,
            float audioStreamReadyDuration, float audioStreamChunkLength,
            RequestProgressDelegate onProgress = null)
        {
            // Audio streaming
#if UNITY_WEBGL
            if (audioStream && audioType != AudioType.OGGVORBIS)
#else
            if (audioStream && audioType != AudioType.OGGVORBIS && audioType != AudioType.UNKNOWN)
#endif
            {
                VLog.W($"Audio streaming not supported by Unity for {(audioType == AudioType.UNKNOWN ? "PCM" : audioType.ToString())}");
                audioStream = false;
            }

            // Add audio download handler
            if (unityRequest.downloadHandler == null)
            {
                // Use default handler
                if (audioType != AudioType.UNKNOWN)
                {
                    unityRequest.downloadHandler = new DownloadHandlerAudioClip(unityRequest.uri, audioType);
                }
                // Assumes Raw PCM
                else if (!audioStream)
                {
                    unityRequest.downloadHandler = new DownloadHandlerBuffer();
                }
                // Assumes Raw PCM
                else
                {
                    AudioStreamData data = new AudioStreamData()
                    {
                        ClipName = WitConstants.ENDPOINT_TTS_CLIP,
                        ClipReadyLength = audioStreamReadyDuration,
                        ClipChunkSize = Mathf.CeilToInt(audioStreamChunkLength * WitConstants.ENDPOINT_TTS_SAMPLE_RATE),
                        DecodeType = AudioStreamDecodeType.PCM16,
                        DecodeChannels = WitConstants.ENDPOINT_TTS_CHANNELS,
                        DecodeSampleRate = WitConstants.ENDPOINT_TTS_SAMPLE_RATE
                    };
                    unityRequest.downloadHandler = new AudioStreamHandler(data);
                }
            }

            // Set stream settings if applicable
            if (unityRequest.downloadHandler is DownloadHandlerAudioClip audioDownloader)
            {
                audioDownloader.streamAudio = audioStream;
            }

            // Perform default request operation
            return Request(unityRequest, (response, error) => OnRequestAudioReady(response, error, onClipReady), onProgress);
        }
        // Called on audio ready to be decoded
        private void OnRequestAudioReady(UnityWebRequest request, string error,
            RequestCompleteDelegate<AudioClip> onClipReady)
        {
            // Check error
            if (!string.IsNullOrEmpty(error))
            {
                onClipReady?.Invoke(null, error);
                return;
            }

            // Get clip
            AudioClip clip = null;
            try
            {
                // Default audio clip handler
                if (request.downloadHandler is DownloadHandlerAudioClip)
                {
                    clip = DownloadHandlerAudioClip.GetContent(request);
                }
                // Custom Raw PCM streaming
                else if (request.downloadHandler is AudioStreamHandler downloadHandlerRaw)
                {
                    clip = downloadHandlerRaw.Clip;
                }
                // Buffer assumes Raw PCM
                else if (request.downloadHandler is DownloadHandlerBuffer)
                {
                    AudioStreamHandler.GetClipFromRawDataAsync(request.downloadHandler.data, AudioStreamDecodeType.PCM16, WitConstants.ENDPOINT_TTS_CLIP,
                        WitConstants.ENDPOINT_TTS_CHANNELS, WitConstants.ENDPOINT_TTS_SAMPLE_RATE, (c, e) => OnRequestAudioDecoded(c, e, onClipReady));
                    return;
                }
            }
            catch (Exception e)
            {
                // Failed to decode audio clip
                onClipReady?.Invoke(null, $"Failed to decode audio clip\n{e}");
                return;
            }

            // Finalize decode
            OnRequestAudioDecoded(clip, error, onClipReady);
        }
        // Callback on audio decode completion
        private void OnRequestAudioDecoded(AudioClip clip, string error,
            RequestCompleteDelegate<AudioClip> onClipReady)
        {
            // Check error
            if (!string.IsNullOrEmpty(error))
            {
                onClipReady?.Invoke(null, error);
                return;
            }

            // Invalid clip
            if (clip != null && (clip.channels == 0 || clip.length == 0f))
            {
                clip.DestroySafely();
                clip = null;
            }

            // Clip is still missing
            if (clip == null)
            {
                onClipReady?.Invoke(null, "Failed to decode empty audio clip");
                return;
            }

            // Return clip
            onClipReady?.Invoke(clip, string.Empty);
        }

        /// <summary>
        /// Request audio clip with url, type, progress delegate & ready delegate
        /// </summary>
        /// <param name="uri">The url to be called</param>
        /// <param name="onClipReady">Called when the clip is ready for playback or has failed to load</param>
        /// <param name="audioType">The audio type requested (Wav, MP3, etc.)</param>
        /// <param name="audioStream">Whether or not audio should be streamed</param>
        /// <param name="onProgress">Clip progress callback</param>
        public bool RequestAudioClip(Uri uri,
            RequestCompleteDelegate<AudioClip> onClipReady,
            AudioType audioType, bool audioStream,
            float audioStreamReadyDuration, float audioStreamChunkLength,
            RequestProgressDelegate onProgress = null)
        {
            UnityWebRequest audioRequest;
            if (audioType == AudioType.UNKNOWN)
            {
                audioRequest = UnityWebRequest.Get(uri);
            }
            else
            {
                audioRequest = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            }
            return RequestAudioClip(audioRequest, onClipReady, audioType, audioStream, audioStreamReadyDuration, audioStreamChunkLength, onProgress);
        }
        #endregion
    }
}
