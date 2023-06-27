/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.WitAi
{
    /// <summary>
    /// Manages a single request lifecycle when sending/receiving data from Wit.ai.
    ///
    /// Note: This is not intended to be instantiated directly. Requests should be created with the
    /// WitRequestFactory
    /// </summary>
    public class WitRequest : VoiceServiceRequest
    {
        #region PARAMETERS
        /// <summary>
        /// The wit Configuration to be used with this request
        /// </summary>
        public WitConfiguration Configuration { get; private set; }
        /// <summary>
        /// The request timeout in ms
        /// </summary>
        public int Timeout { get; private set; } = 1000;
        /// <summary>
        /// Encoding settings for audio based requests
        /// </summary>
        public AudioEncoding AudioEncoding { get; set; }
        [Obsolete("Deprecated for AudioEncoding")]
        public AudioEncoding audioEncoding
        {
            get => AudioEncoding;
            set => AudioEncoding = value;
        }

        /// <summary>
        /// Endpoint to be used for this request
        /// </summary>
        public string Path { get; private set; }
        /// <summary>
        /// Final portion of the endpoint Path
        /// </summary>
        public string Command { get; private set; }
        /// <summary>
        /// Whether a post command should be called
        /// </summary>
        public bool IsPost { get; private set; }
        /// <summary>
        /// Key value pair that is sent as a query param in the Wit.ai uri
        /// </summary>
        [Obsolete("Deprecated for Options.QueryParams")]
        public VoiceServiceRequestOptions.QueryParam[] queryParams
        {
            get
            {
                List<VoiceServiceRequestOptions.QueryParam> results = new List<VoiceServiceRequestOptions.QueryParam>();
                foreach (var key in Options?.QueryParams?.Keys)
                {
                    VoiceServiceRequestOptions.QueryParam p = new VoiceServiceRequestOptions.QueryParam()
                    {
                        key = key,
                        value = Options?.QueryParams[key]
                    };
                    results.Add(p);
                }
                return results.ToArray();
            }
        }

        public byte[] postData;
        public string postContentType;
        public string forcedHttpMethodType = null;
        #endregion PARAMETERS

        #region REQUEST
        /// <summary>
        /// Returns true if the request is being performed
        /// </summary>
        public bool IsRequestStreamActive => IsActive || IsInputStreamReady;
        /// <summary>
        /// Returns true if the response had begun
        /// </summary>
        public bool HasResponseStarted { get; private set; }
        /// <summary>
        /// Returns true if the response had begun
        /// </summary>
        public bool IsInputStreamReady { get; private set; }

        public AudioDurationTracker audioDurationTracker;
        private HttpWebRequest _request;
        private Stream _writeStream;
        private object _streamLock = new object();
        private int _bytesWritten;
        private string _stackTrace;
        private DateTime _requestStartTime;
        private ConcurrentQueue<byte[]> _writeBuffer = new ConcurrentQueue<byte[]>();
        #endregion REQUEST

        #region RESULTS
        /// <summary>
        /// The current status of the request
        /// </summary>
        public string StatusDescription { get; private set; }

        /// <summary>
        /// Simply return the Path to be called
        /// </summary>
        public override string ToString() => Path;

        /// <summary>
        /// Last response data parsed
        /// </summary>
        private WitResponseNode _lastResponseData;
        #endregion RESULTS

        #region EVENTS
        /// <summary>
        /// Provides an opportunity to provide custom headers for the request just before it is
        /// executed.
        /// </summary>
        public event OnProvideCustomHeadersEvent onProvideCustomHeaders;
        public delegate Dictionary<string, string> OnProvideCustomHeadersEvent();
        /// <summary>
        /// Callback called when the server is ready to receive data from the WitRequest's input
        /// stream. See WitRequest.Write()
        /// </summary>
        public event Action<WitRequest> onInputStreamReady;
        /// <summary>
        /// Returns the raw string response that was received before converting it to a JSON object.
        ///
        /// NOTE: This response comes back on a different thread. Do not attempt ot set UI control
        /// values or other interactions from this callback. This is intended to be used for demo
        /// and test UI, not for regular use.
        /// </summary>
        public Action<string> onRawResponse;

        /// <summary>
        /// Provides an opportunity to customize the url just before a request executed
        /// </summary>
        [Obsolete("Deprecated for WitVRequest.OnProvideCustomUri")]
        public OnCustomizeUriEvent onCustomizeUri;
        public delegate Uri OnCustomizeUriEvent(UriBuilder uriBuilder);
        /// <summary>
        /// Allows customization of the request before it is sent out.
        ///
        /// Note: This is for devs who are routing requests to their servers
        /// before sending data to Wit.ai. This allows adding any additional
        /// headers, url modifications, or customization of the request.
        /// </summary>
        public static PreSendRequestDelegate onPreSendRequest;
        public delegate void PreSendRequestDelegate(ref Uri src_uri, out Dictionary<string,string> headers);
        /// <summary>
        /// Returns a partial utterance from an in process request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        [Obsolete("Deprecated for Events.OnPartialTranscription")]
        public event Action<string> onPartialTranscription;
        /// <summary>
        /// Returns a full utterance from a completed request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        [Obsolete("Deprecated for Events.OnFullTranscription")]
        public event Action<string> onFullTranscription;

        /// <summary>
        /// Callback called when a response is received from the server off a partial transcription
        /// </summary>
        [Obsolete("Deprecated for Events.OnPartialResponse")]
        public event Action<WitRequest> onPartialResponse;
        /// <summary>
        /// Callback called when a response is received from the server
        /// </summary>
        [Obsolete("Deprecated for Events.OnComplete")]
        public event Action<WitRequest> onResponse;
        #endregion EVENTS

        #region INITIALIZATION
        /// <summary>
        /// Initialize wit request with configuration & path to endpoint
        /// </summary>
        /// <param name="newConfiguration"></param>
        /// <param name="newOptions"></param>
        /// <param name="newEvents"></param>
        public WitRequest(WitConfiguration newConfiguration, string newPath,
            WitRequestOptions newOptions, VoiceServiceRequestEvents newEvents)
            : base(NLPRequestInputType.Audio, newOptions, newEvents)
        {
            // Set Configuration & path
            Configuration = newConfiguration;
            Path = newPath;

            // Finalize
            _initialized = true;
            SetState(VoiceRequestState.Initialized);
        }
        /// <summary>
        /// Only set state if initialized
        /// </summary>
        private bool _initialized = false;
        protected override void SetState(VoiceRequestState newState)
        {
            if (_initialized)
            {
                base.SetState(newState);
            }
        }

        /// <summary>
        /// Finalize initialization
        /// </summary>
        protected override void OnInit()
        {
            // Determine configuration setting
            Timeout = Configuration == null ? Timeout : Configuration.timeoutMS;

            // Set request settings
            Command = Path.Split('/').First();
            IsPost = WitEndpointConfig.GetEndpointConfig(Configuration).Speech == this.Command
                     || WitEndpointConfig.GetEndpointConfig(Configuration).Dictation == this.Command;

            // Finalize bases
            base.OnInit();
        }
        #endregion INITIALIZATION

        #region AUDIO
        // Handle audio activation
        protected override void HandleAudioActivation()
        {
            SetAudioInputState(VoiceAudioInputState.On);
        }
        // Handle audio deactivation
        protected override void HandleAudioDeactivation()
        {
            // If transmitting,
            if (State == VoiceRequestState.Transmitting)
            {
                CloseRequestStream();
            }
            // Call deactivated
            SetAudioInputState(VoiceAudioInputState.Off);
        }
        #endregion

        #region REQUEST
        // Errors that prevent request submission
        protected override string GetSendError()
        {
            // No configuration found
            if (Configuration == null)
            {
                return "Configuration is not set. Cannot start request.";
            }
            // Cannot start without client access token
            if (string.IsNullOrEmpty(Configuration.GetClientAccessToken()))
            {
                return "Client access token is not defined. Cannot start request.";
            }
            // Cannot perform without input stream delegate
            if (onInputStreamReady == null)
            {
                return "No input stream delegate found";
            }
            // Base
            return base.GetSendError();
        }
        // Simple getter for final uri
        private Uri GetUri()
        {
            // Get query parameters
            Dictionary<string, string> queryParams = new Dictionary<string, string>(Options.QueryParams);

            // Get uri using override
            var uri = WitVRequest.GetWitUri(Configuration, Path, queryParams);
            #pragma warning disable CS0618
            if (onCustomizeUri != null)
            {
                #pragma warning disable CS0618
                uri = onCustomizeUri(new UriBuilder(uri));
            }

            // Return uri
            return uri;
        }
        // Simple getter for final uri
        private Dictionary<string, string> GetHeaders()
        {
            // Get default headers
            Dictionary<string, string> headers = WitVRequest.GetWitHeaders(Configuration, Options?.RequestId, false);

            // Append additional headers
            if (onProvideCustomHeaders != null)
            {
                foreach (OnProvideCustomHeadersEvent e in onProvideCustomHeaders.GetInvocationList())
                {
                    Dictionary<string, string> customHeaders = e();
                    if (customHeaders != null)
                    {
                        foreach (var key in customHeaders.Keys)
                        {
                            headers[key] = customHeaders[key];
                        }
                    }
                }
            }

            // Return headers
            return headers;
        }

        /// <summary>
        /// Start the async request for data from the Wit.ai servers
        /// </summary>
        protected override void HandleSend()
        {
            // Begin
            HasResponseStarted = false;

            // Generate results
            StatusCode = 0;
            StatusDescription = "Starting request";
            _bytesWritten = 0;
            _requestStartTime = DateTime.UtcNow;
            _stackTrace = "-";

            // Get uri & headers
            var uri = GetUri();
            var headers = GetHeaders();

            // Allow overrides
            onPreSendRequest?.Invoke(ref uri, out headers);

            #if UNITY_WEBGL && !UNITY_EDITOR
            StartUnityRequest(uri, headers);
            #else
            #if UNITY_WEBGL && UNITY_EDITOR
            if (IsPost)
            {
                VLog.W("Voice input is not supported in WebGL this functionality is fully enabled at edit time, but may not work at runtime.");
            }
            #endif
            StartThreadedRequest(uri, headers);
            #endif
        }
        #endregion REQUEST

        #region HTTP REQUEST
        /// <summary>
        /// Performs a threaded http request
        /// </summary>
        private void StartThreadedRequest(Uri uri, Dictionary<string, string> headers)
        {
            // Create http web request
            _request = WebRequest.Create(uri.AbsoluteUri) as HttpWebRequest;

            // Off to not wait for a response indefinitely
            _request.KeepAlive = false;

            // Configure request method, content type & chunked
            if (forcedHttpMethodType != null)
            {
                _request.Method = forcedHttpMethodType;
            }
            if (null != postContentType)
            {
                if (forcedHttpMethodType == null) {
                    _request.Method = "POST";
                }
                _request.ContentType = postContentType;
                _request.ContentLength = postData.Length;
            }
            if (IsPost)
            {
                _request.Method = string.IsNullOrEmpty(forcedHttpMethodType) ? "POST" : forcedHttpMethodType;
                _request.ContentType = AudioEncoding.ToString();
                _request.SendChunked = true;
            }

            // Apply user agent
            if (headers.ContainsKey(WitConstants.HEADER_USERAGENT))
            {
                _request.UserAgent = headers[WitConstants.HEADER_USERAGENT];
                headers.Remove(WitConstants.HEADER_USERAGENT);
            }
            // Apply all other headers
            foreach (var key in headers.Keys)
            {
                _request.Headers[key] = headers[key];
            }

            // Apply timeout
            _request.Timeout = Timeout;

            // Begin calling on main thread if needed
            WatchMainThreadCallbacks();

            // Perform http post or put
            if (_request.Method == "POST" || _request.Method == "PUT")
            {
                var getRequestTask = _request.BeginGetRequestStream(HandleWriteStream, _request);
                ThreadPool.RegisterWaitForSingleObject(getRequestTask.AsyncWaitHandle,
                    HandleTimeoutTimer, _request, Timeout, true);
            }
            // Move right to response
            else
            {
                StartResponse();
            }
        }

        // Start response
        private void StartResponse()
        {
            if (_request == null)
            {
                if (StatusCode == 0)
                {
                    StatusCode = WitConstants.ERROR_CODE_GENERAL;
                    StatusDescription = $"Request canceled prior to start";
                }
                HandleNlpResponse(null, StatusDescription);
                return;
            }
            var asyncResult = _request.BeginGetResponse(HandleResponse, _request);
            ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, HandleTimeoutTimer, _request, Timeout, true);
        }

        // Handle timeout callback
        private void HandleTimeoutTimer(object state, bool timeout)
        {
            // Ignore false or too late
            if (!timeout)
            {
                return;
            }

            // No longer active
            StatusCode = WitConstants.ERROR_CODE_TIMEOUT;
            StatusDescription = $"Request timed out after {(DateTime.UtcNow - _requestStartTime).Seconds:0.00} seconds";

            // Clean up the current request if it is still going
            if (null != _request)
            {
                _request.Abort();
            }

            // Close any open stream resources and clean up streaming state flags
            CloseActiveStream();

            // Complete
            MainThreadCallback(() => HandleNlpResponse(null, StatusDescription));
        }

        // Write stream
        private void HandleWriteStream(IAsyncResult ar)
        {
            try
            {
                // Start response stream
                StartResponse();

                // Get write stream
                var stream = _request.EndGetRequestStream(ar);

                // Got write stream
                _bytesWritten = 0;

                // Immediate post
                if (postData != null && postData.Length > 0)
                {
                    Debug.Log("Wrote directly");
                    _bytesWritten += postData.Length;
                    stream.Write(postData, 0, postData.Length);
                    stream.Close();
                }
                // Wait for input stream
                else
                {
                    // Request stream is ready to go
                    IsInputStreamReady = true;
                    _writeStream = stream;

                    // Call input stream ready delegate
                    if (onInputStreamReady != null)
                    {
                        MainThreadCallback(() => onInputStreamReady(this));
                    }
                }
            }
            catch (WebException e)
            {
                // Ignore cancelation errors & if error already occured
                if (e.Status == WebExceptionStatus.RequestCanceled || StatusCode != 0)
                {
                    return;
                }

                // Write stream error
                _stackTrace = e.StackTrace;
                StatusCode = (int) e.Status;
                StatusDescription = e.Message;
                VLog.W(e);
                MainThreadCallback(() => HandleNlpResponse(null, StatusDescription));
            }
            catch (Exception e)
            {
                // Call an error if have not done so yet
                if (StatusCode != 0)
                {
                    return;
                }

                // Non web error occured
                _stackTrace = e.StackTrace;
                StatusCode = WitConstants.ERROR_CODE_GENERAL;
                StatusDescription = e.Message;
                VLog.W(e);
                MainThreadCallback(() => HandleNlpResponse(null, StatusDescription));
            }
        }

        /// <summary>
        /// Write request data to the Wit.ai post's body input stream
        ///
        /// Note: If the stream is not open (IsActive) this will throw an IOException.
        /// Data will be written synchronously. This should not be called from the main thread.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Write(byte[] data, int offset, int length)
        {
            // Ignore without write stream
            if (!IsInputStreamReady || data == null || length == 0)
            {
                return;
            }
            try
            {
                _writeStream.Write(data, offset, length);
                _bytesWritten += length;
                if (audioDurationTracker != null)
                {
                    audioDurationTracker.AddBytes(length);
                }
            }
            catch (ObjectDisposedException e)
            {
                // Handling edge case where stream is closed remotely
                // This problem occurs when the Web server resets or closes the connection after
                // the client application sends the HTTP header.
                // https://support.microsoft.com/en-us/topic/fix-you-receive-a-system-objectdisposedexception-exception-when-you-try-to-access-a-stream-object-that-is-returned-by-the-endgetrequeststream-method-in-the-net-framework-2-0-bccefe57-0a61-517a-5d5f-2dce0cc63265
                VLog.W($"Stream already disposed. It is likely the server reset the connection before streaming started.\n{e}");
                // This prevents a very long holdup on _writeStream.Close
                _writeStream = null;
            }
            catch (IOException e)
            {
                VLog.W(e.Message);
            }
            catch (Exception e)
            {
                VLog.E(e);
            }

            // Perform a cancellation if still waiting for a post
            if (WaitingForPost())
            {
                MainThreadCallback(() => Cancel("Stream was closed with no data written."));
            }
        }

        // Handles response from server
        private void HandleResponse(IAsyncResult asyncResult)
        {
            // Begin response
            HasResponseStarted = true;
            string stringResponse = "";

            try
            {
                // Get response
                CheckStatus();
                using (var response = _request.EndGetResponse(asyncResult))
                {
                    // Got response
                    CheckStatus();
                    HttpWebResponse httpResponse = response as HttpWebResponse;

                    // Apply status & description
                    StatusCode = (int) httpResponse.StatusCode;
                    StatusDescription = httpResponse.StatusDescription;

                    // Get stream
                    using (var responseStream = httpResponse.GetResponseStream())
                    {
                        using (var responseReader = new StreamReader(responseStream))
                        {
                            string chunk;
                            while ((chunk = ReadToDelimiter(responseReader, WitConstants.ENDPOINT_JSON_DELIMITER)) != null)
                            {
                                stringResponse = chunk;
                                ProcessStringResponse(stringResponse);
                            }
                        }
                    }
                }
            }
            catch (JSONParseException e)
            {
                _stackTrace = e.StackTrace;
                StatusCode = WitConstants.ERROR_CODE_INVALID_DATA_FROM_SERVER;
                StatusDescription = "Server returned invalid data.";
                VLog.W(e);
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    // Apply status & error
                    _stackTrace = e.StackTrace;
                    StatusCode = (int) e.Status;
                    StatusDescription = e.Message;
                    VLog.W(e);

                    // Attempt additional parse
                    if (e.Response is HttpWebResponse errorResponse)
                    {
                        StatusCode = (int) errorResponse.StatusCode;
                        try
                        {
                            using (var errorStream = errorResponse.GetResponseStream())
                            {
                                if (errorStream != null)
                                {
                                    using (StreamReader errorReader = new StreamReader(errorStream))
                                    {
                                        stringResponse = errorReader.ReadToEnd();
                                        if (!string.IsNullOrEmpty(stringResponse))
                                        {
                                            ProcessStringResponses(stringResponse);
                                        }
                                    }
                                }
                            }
                        }
                        catch (JSONParseException)
                        {
                            // Response wasn't encoded error, ignore it.
                        }
                        catch (Exception errorResponseError)
                        {
                            // We've already caught that there is an error, we'll ignore any errors
                            // reading error response data and use the status/original error for validation
                            VLog.W(errorResponseError);
                            _stackTrace = e.StackTrace;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _stackTrace = e.StackTrace;
                StatusCode = WitConstants.ERROR_CODE_GENERAL;
                StatusDescription = e.Message;
                VLog.W(e);
            }

            // Close request stream if possible
            CloseRequestStream();

            // Confirm valid response
            if (null != _lastResponseData)
            {
                var error = _lastResponseData["error"];
                if (!string.IsNullOrEmpty(error))
                {
                    // Get code if possible
                    var code = _lastResponseData["code"];
                    if (code != null)
                    {
                        StatusCode = code.AsInt;
                    }
                    // Use general error if code is not provided
                    if (StatusCode == (int)HttpStatusCode.OK)
                    {
                        StatusCode = WitConstants.ERROR_CODE_GENERAL;
                    }
                    // Set error & description
                    if (string.IsNullOrEmpty(StatusDescription))
                    {
                        StatusDescription = $"Error: {code}\n{error}";
                    }
                }
            }
            // Invalid response
            else if (StatusCode == (int)HttpStatusCode.OK)
            {
                StatusCode = WitConstants.ERROR_CODE_NO_DATA_FROM_SERVER;
                StatusDescription = $"Server did not return a valid json response.";
                #if UNITY_EDITOR
                StatusDescription += $"\nActual Response\n{stringResponse}";
                #endif
            }

            // Done
            HasResponseStarted = false;

            MainThreadCallback(() =>
            {
                // Send partial data if not previously sent
                if (!_lastResponseData.HasResponse())
                {
                    ResponseData = _lastResponseData;
                }

                // Apply error if needed
                if (null != _lastResponseData)
                {
                    var error = _lastResponseData["error"];
                    if (!string.IsNullOrEmpty(error))
                    {
                        StatusDescription += $"\n{error}";
                    }
                }

                // Call completion delegate
                HandleNlpResponse(_lastResponseData, StatusCode == (int)HttpStatusCode.OK ? string.Empty : $"{StatusDescription}\n\nStackTrace:\n{_stackTrace}\n\n");
            });
        }
        // Check status
        private void CheckStatus()
        {
            if (StatusCode == 0) return;

            switch (StatusCode)
            {
                case WitConstants.ERROR_CODE_ABORTED:
                    throw new WebException("Request was aborted", WebExceptionStatus.RequestCanceled);
                default:
                    throw new WebException("Status changed before response was received.", (WebExceptionStatus) StatusCode);
            }
        }
        // Read stream until delimiter is hit
        private string ReadToDelimiter(StreamReader reader, string delimiter)
        {
            // Allocate all vars
            StringBuilder results = new StringBuilder();
            int delLength = delimiter.Length;
            int i;
            bool found;
            char nextChar;

            // Iterate each byte in the stream
            while (reader != null && !reader.EndOfStream)
            {
                // Continue until found
                if (reader.Peek() == 0)
                {
                    continue;
                }

                // Append next character
                nextChar = (char)reader.Read();
                results.Append(nextChar);

                // Continue until long as delimiter
                if (results.Length < delLength)
                {
                    continue;
                }

                // Check if string builder ends with delimiter
                found = true;
                for (i=0;i<delLength;i++)
                {
                    // Stop checking if not delimiter
                    if (delimiter[i] != results[results.Length - delLength + i])
                    {
                        found = false;
                        break;
                    }
                }

                // Found delimiter
                if (found)
                {
                    return results.ToString(0, results.Length - delLength);
                }
            }

            // If no delimiter is found, return the rest of the chunk
            return results.Length == 0 ? null : results.ToString();
        }
        // Process individual piece
        private void ProcessStringResponses(string stringResponse)
        {
            // Split by delimiter
            foreach (var stringPart in stringResponse.Split(new string[]{WitConstants.ENDPOINT_JSON_DELIMITER}, StringSplitOptions.RemoveEmptyEntries))
            {
                ProcessStringResponse(stringPart);
            }
        }
        // Safely handles
        private void ProcessStringResponse(string stringResponse)
        {
            // Call raw response for every received response
            if (!string.IsNullOrEmpty(stringResponse))
            {
                MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
            }

            // Decode full response
            WitResponseNode responseNode = WitResponseNode.Parse(stringResponse);
            bool hasResponse = responseNode.HasResponse();
            bool isFinal = responseNode.GetIsFinal();
            string transcription = responseNode.GetTranscription();
            _lastResponseData = responseNode;

            // Apply on main thread
            MainThreadCallback(() =>
            {
                // Set transcription
                if (!string.IsNullOrEmpty(transcription) && (!hasResponse || isFinal))
                {
                    ApplyTranscription(transcription, isFinal);
                }

                // Set response
                if (hasResponse)
                {
                    ResponseData = responseNode;
                }
            });
        }
        // On text change callback
        protected override void OnTranscriptionChanged()
        {
            if (!IsFinalTranscription)
            {
                onPartialTranscription?.Invoke(Transcription);
            }
            else
            {
                onFullTranscription?.Invoke(Transcription);
            }
            base.OnTranscriptionChanged();
        }
        // On response data change callback
        protected override void OnResponseDataChanged()
        {
            onPartialResponse?.Invoke(this);
            base.OnResponseDataChanged();
        }
        // Check if data has been written to post stream while still receiving data
        private bool WaitingForPost()
        {
            return IsPost && _bytesWritten == 0 && StatusCode == 0;
        }
        // Close active stream & then abort if possible
        private void CloseRequestStream()
        {
            // Cancel due to no audio if not an error
            if (WaitingForPost())
            {
                Cancel("Request was closed with no audio captured.");
            }
            // Close
            else
            {
                CloseActiveStream();
            }
        }
        // Close stream
        private void CloseActiveStream()
        {
            IsInputStreamReady = false;
            lock (_streamLock)
            {
                if (null != _writeStream)
                {
                    try
                    {
                        _writeStream.Close();
                    }
                    catch (Exception e)
                    {
                        VLog.W($"Write Stream - Close Failed\n{e}");
                    }
                    _writeStream = null;
                }
            }
        }

        // Perform a cancellation/abort
        protected override void HandleCancel()
        {
            // Close stream
            CloseActiveStream();

            // Apply abort code
            if (StatusCode == 0)
            {
                StatusCode = WitConstants.ERROR_CODE_ABORTED;
                StatusDescription = Results.Message;
            }

            // Abort request
            if (null != _request)
            {
                _request.Abort();
                _request = null;
            }
        }

        // Add response callback & log for abort
        protected override void OnComplete()
        {
            base.OnComplete();

            // Close write stream if still existing
            if (null != _writeStream)
            {
                CloseActiveStream();
            }
            // Abort request if still existing
            if (null != _request)
            {
                _request.Abort();
                _request = null;
            }

            // Finalize response
            onResponse?.Invoke(this);
            onResponse = null;
        }
        #endregion HTTP REQUEST

        #region CALLBACKS
        // Check performing
        private CoroutineUtility.CoroutinePerformer _performer = null;
        // All actions
        private ConcurrentQueue<Action> _mainThreadCallbacks = new ConcurrentQueue<Action>();

        // Called from background thread
        private void MainThreadCallback(Action action)
        {
            if (action == null)
            {
                return;
            }
            _mainThreadCallbacks.Enqueue(action);
        }
        // While active, perform any sent callbacks
        private void WatchMainThreadCallbacks()
        {
            // Ignore if already performing
            if (_performer != null)
            {
                return;
            }

            // Check callbacks every frame (editor or runtime)
            _performer = CoroutineUtility.StartCoroutine(PerformMainThreadCallbacks());
        }
        // Every frame check for callbacks & perform any found
        private System.Collections.IEnumerator PerformMainThreadCallbacks()
        {
            // While checking, continue
            while (HasMainThreadCallbacks())
            {
                // Wait for frame
                if (Application.isPlaying && !Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }
                // Wait for a tick
                else
                {
                    yield return null;
                }

                // Perform if possible
                while (_mainThreadCallbacks.Count > 0 && _mainThreadCallbacks.TryDequeue(out var result))
                {
                    result();
                }
            }
            _performer = null;
        }
        // If active or performing callbacks
        private bool HasMainThreadCallbacks()
        {
            return IsActive || _mainThreadCallbacks.Count > 0;
        }
        #endregion
    }
}
