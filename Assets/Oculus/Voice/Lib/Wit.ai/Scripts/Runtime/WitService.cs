/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Meta.WitAi
{
    public class WitService : MonoBehaviour, IVoiceEventProvider, IVoiceActivationHandler, ITelemetryEventsProvider, IWitRuntimeConfigProvider, IWitConfigurationProvider
    {
        private float _lastMinVolumeLevelTime;
        private WitRequest _recordingRequest;

        private bool _isSoundWakeActive;
        private RingBuffer<byte>.Marker _lastSampleMarker;
        private bool _minKeepAliveWasHit;
        private bool _isActive;
        private long _minSampleByteCount = 1024 * 10;

        private IVoiceEventProvider _voiceEventProvider;
        private ITelemetryEventsProvider _telemetryEventsProvider;
        private IWitRuntimeConfigProvider _runtimeConfigProvider;
        private ITranscriptionProvider _activeTranscriptionProvider;
        private Coroutine _timeLimitCoroutine;
        private IWitRequestProvider _witRequestProvider;

        // Transcription based endpointing
        private bool _receivedTranscription;
        private float _lastWordTime;

        // Parallel Requests
        private HashSet<VoiceServiceRequest> _transmitRequests = new HashSet<VoiceServiceRequest>();
        private Coroutine _queueHandler;

        // Wit configuration provider
        public WitConfiguration Configuration => RuntimeConfiguration?.witConfiguration;

        #region Interfaces
        private IWitByteDataReadyHandler[] _dataReadyHandlers;
        private IWitByteDataSentHandler[] _dataSentHandlers;
        private IDynamicEntitiesProvider[] _dynamicEntityProviders;
        private float _time;

        #endregion

        /// <summary>
        /// Returns true if wit is currently active and listening with the mic
        /// </summary>
        public bool Active => _isActive || IsRequestActive;

        /// <summary>
        /// Active if recording, transmitting, or queued up
        /// </summary>
        public bool IsRequestActive
        {
            get
            {
                if (null != _recordingRequest && _recordingRequest.IsActive)
                {
                    return true;
                }
                return false;
            }
        }

        public IVoiceEventProvider VoiceEventProvider
        {
            get => _voiceEventProvider;
            set => _voiceEventProvider = value;
        }

        public ITelemetryEventsProvider TelemetryEventsProvider
        {
            get => _telemetryEventsProvider;
            set => _telemetryEventsProvider = value;
        }

        public IWitRuntimeConfigProvider ConfigurationProvider
        {
            get => _runtimeConfigProvider;
            set => _runtimeConfigProvider = value;
        }

        public WitRuntimeConfiguration RuntimeConfiguration =>
            _runtimeConfigProvider?.RuntimeConfiguration;

        public VoiceEvents VoiceEvents => _voiceEventProvider.VoiceEvents;

        public TelemetryEvents TelemetryEvents => _telemetryEventsProvider.TelemetryEvents;

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public ITranscriptionProvider TranscriptionProvider
        {
            get => _activeTranscriptionProvider;
            set
            {
                if (null != _activeTranscriptionProvider)
                {
                    _activeTranscriptionProvider.OnFullTranscription.RemoveListener(
                        OnFullTranscription);
                    _activeTranscriptionProvider.OnPartialTranscription.RemoveListener(
                        OnPartialTranscription);
                    _activeTranscriptionProvider.OnMicLevelChanged.RemoveListener(
                        OnTranscriptionMicLevelChanged);
                    _activeTranscriptionProvider.OnStartListening.RemoveListener(
                        OnMicStartListening);
                    _activeTranscriptionProvider.OnStoppedListening.RemoveListener(
                        OnMicStoppedListening);
                }

                _activeTranscriptionProvider = value;

                if (null != _activeTranscriptionProvider)
                {
                    _activeTranscriptionProvider.OnFullTranscription.AddListener(
                        OnFullTranscription);
                    _activeTranscriptionProvider.OnPartialTranscription.AddListener(
                        OnPartialTranscription);
                    _activeTranscriptionProvider.OnMicLevelChanged.AddListener(
                        OnTranscriptionMicLevelChanged);
                    _activeTranscriptionProvider.OnStartListening.AddListener(
                        OnMicStartListening);
                    _activeTranscriptionProvider.OnStoppedListening.AddListener(
                        OnMicStoppedListening);
                }
            }
        }

        public IWitRequestProvider WitRequestProvider
        {
            get => _witRequestProvider;
            set => _witRequestProvider = value;
        }

        public bool MicActive => AudioBuffer.Instance.IsRecording(this);

        protected bool ShouldSendMicData => RuntimeConfiguration.sendAudioToWit ||
                                                  null == _activeTranscriptionProvider;

        /// <summary>
        /// Check configuration, client access token & app id
        /// </summary>
        public virtual bool IsConfigurationValid()
        {
            return RuntimeConfiguration.witConfiguration != null &&
                   !string.IsNullOrEmpty(RuntimeConfiguration.witConfiguration.GetClientAccessToken());
        }

        #region LIFECYCLE
        // Find transcription provider & Mic
        protected void Awake()
        {
            _dataReadyHandlers = GetComponents<IWitByteDataReadyHandler>();
            _dataSentHandlers = GetComponents<IWitByteDataSentHandler>();
        }
        // Add mic delegates
        protected void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _runtimeConfigProvider = GetComponent<IWitRuntimeConfigProvider>();
            _voiceEventProvider = GetComponent<IVoiceEventProvider>();

            if (null == _activeTranscriptionProvider && null != RuntimeConfiguration &&
                RuntimeConfiguration.customTranscriptionProvider)
            {
                TranscriptionProvider = RuntimeConfiguration.customTranscriptionProvider;
            }

            SetMicDelegates(true);

            _dynamicEntityProviders = GetComponents<IDynamicEntitiesProvider>();
        }
        // Remove mic delegates
        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioBufferEvents e = AudioBuffer.Instance?.Events;
            SetMicDelegates(false);
        }
        // On scene refresh
        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetMicDelegates(true);
        }
        // Toggle audio events
        private AudioBuffer _buffer;
        private bool _bufferDelegates = false;
        protected void SetMicDelegates(bool add)
        {
            // Obtain buffer
            if (_buffer == null)
            {
                _buffer = AudioBuffer.Instance;
                _bufferDelegates = false;
            }
            // Get events if possible
            AudioBufferEvents e = _buffer?.Events;
            if (e == null)
            {
                return;
            }
            // Already set
            if (_bufferDelegates == add)
            {
                return;
            }
            // Set delegates
            _bufferDelegates = add;

            // Add delegates
            if (add)
            {
                e.OnMicLevelChanged.AddListener(OnMicLevelChanged);
                e.OnByteDataReady.AddListener(OnByteDataReady);
                e.OnSampleReady += OnMicSampleReady;
            }
            // Remove delegates
            else
            {
                e.OnMicLevelChanged.RemoveListener(OnMicLevelChanged);
                e.OnByteDataReady.RemoveListener(OnByteDataReady);
                e.OnSampleReady -= OnMicSampleReady;
            }
        }
        #endregion

        #region ACTIVATION
        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public void Activate() => Activate(new WitRequestOptions());
        public void Activate(WitRequestOptions requestOptions) => Activate(requestOptions, new VoiceServiceRequestEvents());
        public VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            // Not valid
            if (!IsConfigurationValid())
            {
                VLog.E($"Your AppVoiceExperience \"{gameObject.name}\" does not have a wit config assigned. Understanding Viewer activations will not trigger in game events..");
                return null;
            }
            // Already recording
            if (_isActive)
            {
                return null;
            }

            // Stop recording
            StopRecording();

            // Now active
            _isActive = true;
            _lastSampleMarker = AudioBuffer.Instance.CreateMarker(ConfigurationProvider.RuntimeConfiguration.preferredActivationOffset);
            _lastMinVolumeLevelTime = float.PositiveInfinity;
            _lastWordTime = float.PositiveInfinity;
            _receivedTranscription = false;

            // Generate request
            WitRequest request = WitRequestProvider != null ? WitRequestProvider.CreateWitRequest(RuntimeConfiguration.witConfiguration, requestOptions, requestEvents, _dynamicEntityProviders)
                : RuntimeConfiguration.witConfiguration.CreateSpeechRequest(requestOptions, requestEvents, _dynamicEntityProviders);
            SetupRequest(request);

            // Start recording if possible
            if (ShouldSendMicData)
            {
                if (!AudioBuffer.Instance.IsRecording(this))
                {
                    _minKeepAliveWasHit = false;
                    _isSoundWakeActive = true;
                    StartRecording();
                }
                _recordingRequest.ActivateAudio();
            }

            // Activate transcription provider
            _activeTranscriptionProvider?.Activate();

            // Return the generated request
            return _recordingRequest;
        }
        /// <summary>
        /// Activate the microphone and immediately send data to Wit for NLU processing.
        /// </summary>
        public void ActivateImmediately() => ActivateImmediately(new WitRequestOptions());
        public void ActivateImmediately(WitRequestOptions requestOptions) => ActivateImmediately(requestOptions, new VoiceServiceRequestEvents());
        public VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            // Activate mic & generate request if possible
            var request = Activate(requestOptions, requestEvents);
            if (request == null)
            {
                return null;
            }

            // Send recording request
            SendRecordingRequest();

            // Start marker
            _lastSampleMarker = AudioBuffer.Instance.CreateMarker(ConfigurationProvider
                .RuntimeConfiguration.preferredActivationOffset);

            // Return the request
            return request;
        }
        /// <summary>
        /// Sends recording request if possible
        /// </summary>
        protected virtual void SendRecordingRequest()
        {
            if (_recordingRequest == null || _recordingRequest.State != VoiceRequestState.Initialized)
            {
                return;
            }

            // Sound wake active
            _isSoundWakeActive = false;

            // Execute request
            if (ShouldSendMicData)
            {
                ExecuteRequest(_recordingRequest);
            }
        }
        /// <summary>
        /// Setup recording request
        /// </summary>
        /// <param name="recordingRequest"></param>
        protected void SetupRequest(WitRequest newRequest)
        {
            if (_recordingRequest == newRequest)
            {
                return;
            }

            // Set request & events
            _recordingRequest = newRequest;
            _recordingRequest.Events.OnCancel.AddListener(HandleResult);
            _recordingRequest.Events.OnFailed.AddListener(HandleResult);
            _recordingRequest.Events.OnSuccess.AddListener(HandleResult);
            _recordingRequest.Events.OnComplete.AddListener(HandleComplete);

            // Call service events
            VoiceEvents.OnRequestOptionSetup?.Invoke(_recordingRequest.Options);
            VoiceEvents.OnRequestInitialized?.Invoke(_recordingRequest);
        }
        /// <summary>
        /// Execute a wit request immediately
        /// </summary>
        /// <param name="recordingRequest"></param>
        public void ExecuteRequest(WitRequest newRequest)
        {
            SetupRequest(newRequest);
            newRequest.AudioEncoding = AudioBuffer.Instance.AudioEncoding;
            newRequest.audioDurationTracker = new AudioDurationTracker(_recordingRequest.Options?.RequestId,
                newRequest.AudioEncoding);
            newRequest.onInputStreamReady += r => OnWitReadyForData();
            _recordingRequest.Events.OnPartialTranscription.AddListener(OnPartialTranscription);
            _recordingRequest.Events.OnFullTranscription.AddListener(OnFullTranscription);
            _recordingRequest.Events.OnPartialResponse.AddListener(HandlePartialResult);
            #pragma warning disable CS0618
            VoiceEvents.OnRequestCreated?.Invoke(_recordingRequest);
            VoiceEvents.OnSend?.Invoke(_recordingRequest);
            _timeLimitCoroutine = StartCoroutine(DeactivateDueToTimeLimit());
            _recordingRequest.Send();
        }
        #endregion

        #region TEXT REQUESTS
        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public void Activate(string text) => Activate(text, new WitRequestOptions());
        public void Activate(string text, WitRequestOptions requestOptions) => Activate(text, requestOptions, new VoiceServiceRequestEvents());
        public VoiceServiceRequest Activate(string text, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            // Not valid
            if (!IsConfigurationValid())
            {
                VLog.E($"Your AppVoiceExperience \"{gameObject.name}\" does not have a wit config assigned. Understanding Viewer activations will not trigger in game events..");
                return null;
            }

            // Handle option setup
            VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);

            // Generate request
            VoiceServiceRequest request = Configuration.CreateMessageRequest(requestOptions, requestEvents, _dynamicEntityProviders);
            request.Events.OnCancel.AddListener(HandleResult);
            request.Events.OnFailed.AddListener(HandleResult);
            request.Events.OnSuccess.AddListener(HandleResult);
            request.Events.OnComplete.AddListener(HandleComplete);
            _transmitRequests.Add(request);

            // Call on create delegates
            VoiceEvents?.OnRequestInitialized?.Invoke(request);
            #pragma warning disable CS0618
            VoiceEvents?.OnRequestCreated?.Invoke(null);
            VoiceEvents?.OnSend?.Invoke(request);

            // Send & return
            request.Send(text);
            return request;
        }
        #endregion TEXT REQUESTS

        #region RECORDING
        // Stop any recording
        private void StopRecording()
        {
            if (!AudioBuffer.Instance.IsRecording(this)) return;
            AudioBuffer.Instance.StopRecording(this);
        }
        // When wit is ready, start recording
        private void OnWitReadyForData()
        {
            _lastMinVolumeLevelTime = _time;
            if (!AudioBuffer.Instance.IsRecording(this))
            {
                StartRecording();
            }
        }
        // Handle begin recording
        private void StartRecording()
        {
            // Check for input
            if (!AudioBuffer.Instance.IsInputAvailable)
            {
                AudioBuffer.Instance.CheckForInput();
            }
            // Wait for input and then try again
            if (!AudioBuffer.Instance.IsInputAvailable)
            {
                VoiceEvents.OnError.Invoke("Input Error", "No input source was available. Cannot activate for voice input.");
                return;
            }
            // Already recording
            if (AudioBuffer.Instance.IsRecording(this))
            {
                return;
            }

            // Start recording
            AudioBuffer.Instance.StartRecording(this);
        }
        // Callback for mic start
        private void OnMicStartListening()
        {
            VoiceEvents?.OnStartListening?.Invoke();
        }
        // Callback for mic end
        private void OnMicStoppedListening()
        {
            VoiceEvents?.OnStoppedListening?.Invoke();
        }
        // Callback for mic byte data ready
        private void OnByteDataReady(byte[] buffer, int offset, int length)
        {
            VoiceEvents?.OnByteDataReady.Invoke(buffer, offset, length);

            for (int i = 0; null != _dataReadyHandlers && i < _dataReadyHandlers.Length; i++)
            {
                _dataReadyHandlers[i].OnWitDataReady(buffer, offset, length);
            }
        }
        // Callback for mic sample data ready
        private void OnMicSampleReady(RingBuffer<byte>.Marker marker, float levelMax)
        {
            if (null == _lastSampleMarker || _recordingRequest == null) return;

            if (_minSampleByteCount > _lastSampleMarker.RingBuffer.Capacity)
            {
                _minSampleByteCount = _lastSampleMarker.RingBuffer.Capacity;
            }

            if (_recordingRequest.State == VoiceRequestState.Transmitting && _recordingRequest.IsInputStreamReady && _lastSampleMarker.AvailableByteCount >= _minSampleByteCount)
            {
                // Flush the marker since the last read and send it to Wit
                _lastSampleMarker.ReadIntoWriters(
                    (buffer, offset, length) =>
                    {
                        _recordingRequest.Write(buffer, offset, length);
                    },
                    (buffer, offset, length) => VoiceEvents?.OnByteDataSent?.Invoke(buffer, offset, length),
                    (buffer, offset, length) =>
                    {
                        for (int i = 0; i < _dataSentHandlers.Length; i++)
                        {
                            _dataSentHandlers[i]?.OnWitDataSent(buffer, offset, length);
                        }
                    });

                if (_receivedTranscription)
                {
                    float elapsed = _time - _lastWordTime;
                    if (elapsed >
                        RuntimeConfiguration.minTranscriptionKeepAliveTimeInSeconds)
                    {
                        VLog.D($"Deactivated due to inactivity. No new words detected in {elapsed:0.00} seconds.");
                        DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToInactivity);
                    }
                }
                else
                {
                    float elapsed = _time - _lastMinVolumeLevelTime;
                    if (elapsed >
                        RuntimeConfiguration.minKeepAliveTimeInSeconds)
                    {
                        VLog.D($"Deactivated due to inactivity. No sound detected in {elapsed:0.00} seconds.");
                        DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToInactivity);
                    }
                }
            }
            else if (_isSoundWakeActive && levelMax > RuntimeConfiguration.soundWakeThreshold)
            {
                VoiceEvents?.OnMinimumWakeThresholdHit?.Invoke();
                SendRecordingRequest();
                _lastSampleMarker.Offset(RuntimeConfiguration.sampleLengthInMs * -2);
            }
        }
        // Time tracking for multi-threaded callbacks
        private void Update()
        {
            _time = Time.time;
        }
        // Mic level change
        private void OnMicLevelChanged(float level)
        {
            if (null != TranscriptionProvider && TranscriptionProvider.OverrideMicLevel) return;

            if (level > RuntimeConfiguration.minKeepAliveVolume)
            {
                _lastMinVolumeLevelTime = _time;
                _minKeepAliveWasHit = true;
            }
            VoiceEvents?.OnMicLevelChanged?.Invoke(level);
        }
        // Mic level changed in transcription
        private void OnTranscriptionMicLevelChanged(float level)
        {
            if (null != TranscriptionProvider && TranscriptionProvider.OverrideMicLevel)
            {
                OnMicLevelChanged(level);
            }
        }
        // AudioDurationTracker
        private void FinalizeAudioDurationTracker()
        {
            AudioDurationTracker audioDurationTracker = _recordingRequest?.audioDurationTracker;
            if (audioDurationTracker == null)
            {
                return;
            }

            if (null == _recordingRequest)
            {
                VLog.W($"Missing request for recording.");
                return;
            }

            string requestId = _recordingRequest.Options?.RequestId;
            if (!string.Equals(requestId, audioDurationTracker.GetRequestId()))
            {
                VLog.W($"Mismatch in request IDs when finalizing AudioDurationTracker. " +
                       $"Expected {requestId} but got {audioDurationTracker.GetRequestId()}");
                return;
            }
            audioDurationTracker.FinalizeAudio();
            TelemetryEvents.OnAudioTrackerFinished?.Invoke(audioDurationTracker.GetFinalizeTimeStamp(), audioDurationTracker.GetAudioDuration());
        }
        #endregion

        #region DEACTIVATION
        /// <summary>
        /// Stop listening and submit the collected microphone data to wit for processing.
        /// </summary>
        public void Deactivate()
        {
            DeactivateRequest(AudioBuffer.Instance.IsRecording(this) ? VoiceEvents?.OnStoppedListeningDueToDeactivation : null, false);
        }

        /// <summary>
        /// Stop listening and cancel a specific report
        /// </summary>
        public void DeactivateAndAbortRequest(VoiceServiceRequest request)
        {
            if (request != null)
            {
                VoiceEvents?.OnAborting?.Invoke();
                request.Cancel();
            }
        }
        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        public void DeactivateAndAbortRequest()
        {
            DeactivateRequest(AudioBuffer.Instance.IsRecording(this) ? VoiceEvents?.OnStoppedListeningDueToDeactivation : null, true);
        }
        // Stop listening if time expires
        private IEnumerator DeactivateDueToTimeLimit()
        {
            yield return new WaitForSeconds(RuntimeConfiguration.maxRecordingTime);
            if (IsRequestActive)
            {
                VLog.D($"Deactivated input due to timeout.\nMax Record Time: {RuntimeConfiguration.maxRecordingTime}");
                DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToTimeout, false);
            }
        }
        private void DeactivateRequest(UnityEvent onComplete = null, bool abort = false)
        {
            // Aborting
            if (abort)
            {
                VoiceEvents?.OnAborting?.Invoke();
            }

            // Stop timeout coroutine
            if (null != _timeLimitCoroutine)
            {
                StopCoroutine(_timeLimitCoroutine);
                _timeLimitCoroutine = null;
            }

            // No longer active
            _isActive = false;

            // Stop recording
            StopRecording();
            FinalizeAudioDurationTracker();

            // Deactivate transcription provider
            _activeTranscriptionProvider?.Deactivate();

            // Deactivate recording request
            WitRequest previousRequest = _recordingRequest;
            _recordingRequest = null;
            DeactivateWitRequest(previousRequest, abort);

            // Abort transmitting requests
            if (abort)
            {
                HashSet<VoiceServiceRequest> requests = _transmitRequests;
                _transmitRequests = new HashSet<VoiceServiceRequest>();
                foreach (var request in requests)
                {
                    DeactivateWitRequest(request, true);
                }
            }
            // Transmit recording request
            else if (previousRequest != null && previousRequest.IsActive && _minKeepAliveWasHit)
            {
                _transmitRequests.Add(_recordingRequest);
                _recordingRequest = null;
                VoiceEvents?.OnMicDataSent?.Invoke();
            }
            // Disable below event
            _minKeepAliveWasHit = false;

            // Perform on complete event
            onComplete?.Invoke();
        }
        // Deactivate wit request
        private void DeactivateWitRequest(VoiceServiceRequest request, bool abort)
        {
            if (request == null)
            {
                return;
            }
            if (abort)
            {
                request.Cancel("Request was aborted by user.");
            }
            else
            {
                request.DeactivateAudio();
            }
        }
        #endregion

        #region TRANSCRIPTION
        private void OnPartialTranscription(string transcription)
        {
            _receivedTranscription = true;
            _lastWordTime = _time;
            VoiceEvents?.OnPartialTranscription.Invoke(transcription);
        }
        private void OnFullTranscription(string transcription)
        {
            VoiceEvents?.OnFullTranscription?.Invoke(transcription);
        }
        #endregion

        #region RESPONSE
        /// <summary>
        /// Main thread call to handle partial response callbacks
        /// </summary>
        private void HandlePartialResult(WitResponseNode response)
        {
            if (response != null)
            {
                VoiceEvents?.OnPartialResponse?.Invoke(response);
            }
        }
        /// <summary>
        /// Main thread call to handle result callbacks
        /// </summary>
        private void HandleResult(VoiceServiceRequest request)
        {
            // If result is obtained before transcription
            if (request == _recordingRequest)
            {
                DeactivateRequest(null, false);
            }

            // Handle Success
            if (request.State == VoiceRequestState.Successful)
            {
                VLog.D("Request Success");
                VoiceEvents?.OnResponse?.Invoke(request.Results.ResponseData);
                VoiceEvents?.OnRequestCompleted?.Invoke();
            }
            // Handle Cancellation
            else if (request.State == VoiceRequestState.Canceled)
            {
                VLog.D($"Request Canceled\nReason: {request.Results.Message}");
                VoiceEvents?.OnCanceled?.Invoke(request.Results.Message);
                if (!string.Equals(request.Results.Message, WitConstants.CANCEL_MESSAGE_PRE_SEND))
                {
                    VoiceEvents?.OnAborted?.Invoke();
                }
            }
            // Handle Failure
            else if (request.State == VoiceRequestState.Failed)
            {
                VLog.D($"Request Failed\nError: {request.Results.Message}");
                VoiceEvents?.OnError?.Invoke("HTTP Error " + request.Results.StatusCode, request.Results.Message);
                VoiceEvents?.OnRequestCompleted?.Invoke();
            }
            // Remove from transmit list, missing if aborted
            if ( _transmitRequests.Contains(request))
            {
                _transmitRequests.Remove(request);
            }
        }
        /// <summary>
        /// Handle request completion
        /// </summary>
        private void HandleComplete(VoiceServiceRequest request)
        {
            VoiceEvents?.OnComplete?.Invoke(request);
        }
        #endregion
    }

    public interface IWitRuntimeConfigProvider
    {
        WitRuntimeConfiguration RuntimeConfiguration { get; }
    }

    public interface IVoiceEventProvider
    {
        VoiceEvents VoiceEvents { get; }
    }

    public interface ITelemetryEventsProvider
    {
        TelemetryEvents TelemetryEvents { get; }
    }
}
