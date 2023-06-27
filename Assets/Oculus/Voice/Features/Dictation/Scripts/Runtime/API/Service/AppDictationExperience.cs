/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Globalization;
using Meta.Voice;
using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Dictation;
using Meta.WitAi.Dictation.Data;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using Oculus.Voice.Dictation.Bindings.Android;
using Oculus.VoiceSDK.Utilities;
using Oculus.Voice.Core.Bindings.Android.PlatformLogger;
using Oculus.Voice.Core.Bindings.Interfaces;
using UnityEngine;

namespace Oculus.Voice.Dictation
{
    public class AppDictationExperience : DictationService, IWitRuntimeConfigProvider, IWitConfigurationProvider
    {
        [SerializeField] private WitDictationRuntimeConfiguration runtimeConfiguration;
        [Tooltip("Uses platform dictation service instead of accessing wit directly from within the application.")]
        [SerializeField] private bool usePlatformServices;

        [Tooltip("Dictation will not fallback to Wit if platform dictation is not available. Not applicable in Unity Editor")]
        [SerializeField] private bool doNotFallbackToWit;
        [Tooltip("Enables logs related to the interaction to be displayed on console")]
        [SerializeField] private bool enableConsoleLogging;

        public WitRuntimeConfiguration RuntimeConfiguration => runtimeConfiguration;
        public WitDictationRuntimeConfiguration RuntimeDictationConfiguration
        {
            get => runtimeConfiguration;
            set => runtimeConfiguration = value;
        }
        public WitConfiguration Configuration => RuntimeConfiguration?.witConfiguration;

        private IDictationService _dictationServiceImpl;
        private IVoiceSDKLogger _voiceSDKLogger;
        /// <summary>
        /// True if the user currently has requested dictation to be active. This will remain true until a Deactivate
        /// method is called and we will reactivate when the mic stops as a result.
        /// </summary>
        private bool _isActive;

        private DictationSession _activeSession;
        private WitRequestOptions _activeRequestOptions;

        public DictationSession ActiveSession => _activeSession;
        public WitRequestOptions ActiveRequestOptions => _activeRequestOptions;

        public event Action OnInitialized;

#if UNITY_ANDROID && !UNITY_EDITOR
        // This version is auto-updated for a release build
        private readonly string PACKAGE_VERSION = "54.0.0.135.284";
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        public bool HasPlatformIntegrations => usePlatformServices && _dictationServiceImpl is PlatformDictationImpl;
#else
        public bool HasPlatformIntegrations => false;
#endif

        public bool UsePlatformIntegrations
        {
            get => usePlatformServices;
            set
            {
                // If we're trying to turn on platform services and they're not currently active we
                // will forcably reinit and try to set the state.
                if (usePlatformServices != value || HasPlatformIntegrations != value)
                {
                    usePlatformServices = value;
#if UNITY_ANDROID && !UNITY_EDITOR
                    VLog.D($"{(usePlatformServices ? "Enabling" : "Disabling")} platform integration.");
                    InitDictation();
#endif
                }
            }
        }

        public bool DoNotFallbackToWit
        {
            get => doNotFallbackToWit;
            set => doNotFallbackToWit = value;
        }

        private void InitDictation()
        {
            // Clean up if we're switching to native C# wit impl
            if (!UsePlatformIntegrations)
            {
                if (_dictationServiceImpl is PlatformDictationImpl)
                {
                    ((PlatformDictationImpl) _dictationServiceImpl).Disconnect();
                }

                if (_voiceSDKLogger is VoiceSDKPlatformLoggerImpl)
                {
                    try
                    {
                        ((VoiceSDKPlatformLoggerImpl)_voiceSDKLogger).Disconnect();
                    }
                    catch (Exception e)
                    {
                        VLog.E($"Disconnection error: {e.Message}");
                    }
                }
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            var loggerImpl = new VoiceSDKPlatformLoggerImpl();
            loggerImpl.Connect(PACKAGE_VERSION);
            _voiceSDKLogger = loggerImpl;

            if (UsePlatformIntegrations)
            {
                VLog.D("Checking platform dictation capabilities...");
                var platformDictationImpl = new PlatformDictationImpl(this);
                platformDictationImpl.OnServiceNotAvailableEvent += OnPlatformServiceNotAvailable;
                platformDictationImpl.Connect(PACKAGE_VERSION);
                if (platformDictationImpl.PlatformSupportsDictation)
                {
                    _dictationServiceImpl = platformDictationImpl;
                    _dictationServiceImpl.DictationEvents = DictationEvents;
                    _dictationServiceImpl.TelemetryEvents = TelemetryEvents;
                    platformDictationImpl.SetDictationRuntimeConfiguration(RuntimeDictationConfiguration);
                    VLog.D("Dictation platform init complete");
                    _voiceSDKLogger.IsUsingPlatformIntegration = true;
                }
                else
                {
                    OnPlatformServiceNotAvailable();
                }
            }
            else
            {
                RevertToWitDictation();
            }
#else
            _voiceSDKLogger = new VoiceSDKConsoleLoggerImpl();
            RevertToWitDictation();
#endif
            _voiceSDKLogger.WitApplication = RuntimeDictationConfiguration?.witConfiguration?.GetLoggerAppId();
            _voiceSDKLogger.ShouldLogToConsole = enableConsoleLogging;

            OnInitialized?.Invoke();
        }

        private void OnPlatformServiceNotAvailable()
        {
#if !UNITY_EDITOR
            if (DoNotFallbackToWit)
            {
                VLog.D("Platform dictation service unavailable. Falling back to WitDictation is disabled");
                DictationEvents.OnError?.Invoke("Platform dictation unavailable", "Platform dictation service is not available");
                return;
            }
#endif

            VLog.D("Platform dictation service unavailable. Falling back to WitDictation");
            RevertToWitDictation();
        }

        private void OnDictationServiceNotAvailable()
        {
            VLog.D("Dictation service unavailable");
            DictationEvents.OnError?.Invoke("Dictation unavailable", "Dictation service is not available");
        }

        private void RevertToWitDictation()
        {
            WitDictation witDictation = GetComponent<WitDictation>();
            if (null == witDictation)
            {
                witDictation = gameObject.AddComponent<WitDictation>();
                witDictation.hideFlags = HideFlags.HideInInspector;
            }

            witDictation.RuntimeConfiguration = RuntimeDictationConfiguration;
            witDictation.DictationEvents = DictationEvents;
            witDictation.TelemetryEvents = TelemetryEvents;
            _dictationServiceImpl = witDictation;
            VLog.D("WitDictation init complete");
            _voiceSDKLogger.IsUsingPlatformIntegration = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MicPermissionsManager.HasMicPermission())
            {
                InitDictation();
            }
            else
            {
                MicPermissionsManager.RequestMicPermission((e) => InitDictation());
            }

            DictationEvents.OnStartListening.AddListener(OnStarted);
            DictationEvents.OnStoppedListening.AddListener(OnStopped);
            DictationEvents.OnComplete.AddListener(OnComplete);
            DictationEvents.OnDictationSessionStarted.AddListener(OnDictationSessionStarted);
            DictationEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
            TelemetryEvents.OnAudioTrackerFinished.AddListener(OnAudioDurationTrackerFinished);
        }

        protected override void OnDisable()
        {
#if UNITY_ANDROID
            if (_dictationServiceImpl is PlatformDictationImpl platformDictationImpl)
            {
                platformDictationImpl.Disconnect();
            }

            if (_voiceSDKLogger is VoiceSDKPlatformLoggerImpl loggerImpl)
            {
                loggerImpl.Disconnect();
            }
#endif
            _dictationServiceImpl = null;
            _voiceSDKLogger = null;
            DictationEvents.OnStartListening.RemoveListener(OnStarted);
            DictationEvents.OnStoppedListening.RemoveListener(OnStopped);
            DictationEvents.OnComplete.RemoveListener(OnComplete);
            DictationEvents.OnDictationSessionStarted.RemoveListener(OnDictationSessionStarted);
            DictationEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            DictationEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            TelemetryEvents.OnAudioTrackerFinished.RemoveListener(OnAudioDurationTrackerFinished);
            base.OnDisable();
        }

        #region DictationService properties

        public override bool Active => _dictationServiceImpl != null && _dictationServiceImpl.Active;
        public override bool IsRequestActive => _dictationServiceImpl != null && _dictationServiceImpl.IsRequestActive;

        public override ITranscriptionProvider TranscriptionProvider
        {
            get => _dictationServiceImpl.TranscriptionProvider;
            set => _dictationServiceImpl.TranscriptionProvider = value;

        }
        public override bool MicActive => null != _dictationServiceImpl && _dictationServiceImpl.MicActive;
        protected override bool ShouldSendMicData => RuntimeConfiguration.sendAudioToWit ||
                                                     null == TranscriptionProvider;
        #endregion

        #region DictationService APIs
        /// <summary>
        /// Toggle dictation activation from on->off or off->on depending on the current active state.
        /// </summary>
        public void Toggle()
        {
            if(Active) Deactivate();
            else Activate();
        }

        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public override VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            if (_dictationServiceImpl == null)
            {
                OnDictationServiceNotAvailable();
                return null;
            }

            if (null == requestOptions) requestOptions = new WitRequestOptions();

            if (!_isActive)
            {
                _activeSession = new DictationSession();
                DictationEvents.OnDictationSessionStarted.Invoke(_activeSession);
            }

            _activeRequestOptions = requestOptions;
            _isActive = true;
            _voiceSDKLogger.LogInteractionStart(requestOptions.RequestId, "dictation");
            LogRequestConfig();
            return _dictationServiceImpl.Activate(requestOptions, requestEvents);
        }

        /// <summary>
        /// Activates immediately and starts sending data to the server. This will not wait for min wake threshold
        /// </summary>
        /// <param name="options"></param>
        public override VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            if (_dictationServiceImpl == null)
            {
                OnDictationServiceNotAvailable();
                return null;
            }

            if (!_isActive)
            {
                _activeSession = new DictationSession();
                DictationEvents.OnDictationSessionStarted.Invoke(_activeSession);
            }

            _activeRequestOptions = requestOptions;
            _isActive = true;
            _voiceSDKLogger.LogInteractionStart(requestOptions.RequestId, "dictation");
            LogRequestConfig();
            return _dictationServiceImpl.ActivateImmediately(requestOptions, requestEvents);
        }

        /// <summary>
        /// Deactivates. If a transcription is in progress the network request will complete and any additional
        /// transcription values will be returned.
        /// </summary>
        public override void Deactivate()
        {
            if (_dictationServiceImpl == null)
            {
                OnDictationServiceNotAvailable();
                return;
            }

            _isActive = false;
            _dictationServiceImpl.Deactivate();
        }

        /// <summary>
        /// Deactivates and ignores any pending transcription content.
        /// </summary>
        public override void Cancel()
        {
            if (_dictationServiceImpl == null)
            {
                OnDictationServiceNotAvailable();
                return;
            }

            _dictationServiceImpl.Cancel();
            CleanupSession();
        }
        #endregion

        #region Listeners for logging

        void OnStarted()
        {
            _voiceSDKLogger.LogInteractionPoint("startedListening");
        }

        void OnStopped()
        {
            _voiceSDKLogger.LogInteractionPoint("stoppedListening");

            if (RuntimeDictationConfiguration.dictationConfiguration.multiPhrase && _isActive)
            {
                Activate(_activeRequestOptions);
            }
        }

        void OnDictationSessionStarted(DictationSession session)
        {
            if (session is PlatformDictationSession platformDictationSession)
            {
                _activeSession = session;
                _voiceSDKLogger.LogAnnotation("platformInteractionId", platformDictationSession.platformSessionId);
            }
        }

        void OnAudioDurationTrackerFinished(long timestamp, double audioDuration)
        {
            _voiceSDKLogger.LogAnnotation("adt_duration", audioDuration.ToString(CultureInfo.InvariantCulture));
            _voiceSDKLogger.LogAnnotation("adt_finished", timestamp.ToString());
        }

        void OnPartialTranscription(string text)
        {
            _voiceSDKLogger.LogFirstTranscriptionTime();
        }

        void OnFullTranscription(string text)
        {
            _voiceSDKLogger.LogInteractionPoint("fullTranscriptionTime");
        }

        void OnComplete(VoiceServiceRequest request)
        {
            if (request.State == VoiceRequestState.Failed)
            {
                VLog.E($"Dictation Request Failed\nError: {request.Results.Message}");
                _voiceSDKLogger.LogInteractionEndFailure(request.Results.Message);
            }
            else if (request.State == VoiceRequestState.Canceled)
            {
                VLog.W($"Dictation Request Canceled\nMessage: {request.Results.Message}");
                _voiceSDKLogger.LogInteractionEndFailure("aborted");
            }
            else
            {
                VLog.D($"Dictation Request Success");
                var tokens = request.ResponseData?["speech"]?["tokens"];
                if (tokens != null)
                {
                    int speechTokensLength = tokens.Count;
                    string speechLength = request.ResponseData["speech"]["tokens"][speechTokensLength - 1]?["end"]?.Value;
                    _voiceSDKLogger.LogAnnotation("audioLength", speechLength);
                }
                _voiceSDKLogger.LogInteractionEndSuccess();
            }
            if (!_isActive)
            {
                DictationEvents.OnDictationSessionStopped?.Invoke(_activeSession);
                CleanupSession();
            }
        }

        void LogRequestConfig()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _voiceSDKLogger.LogAnnotation("clientSDKVersion", PACKAGE_VERSION);
#endif
            _voiceSDKLogger.LogAnnotation("minWakeThreshold",
                RuntimeConfiguration?.soundWakeThreshold.ToString(CultureInfo.InvariantCulture));
            _voiceSDKLogger.LogAnnotation("minKeepAliveTimeSec",
                RuntimeConfiguration?.minKeepAliveTimeInSeconds.ToString(CultureInfo.InvariantCulture));
            _voiceSDKLogger.LogAnnotation("minTranscriptionKeepAliveTimeSec",
                RuntimeConfiguration?.minTranscriptionKeepAliveTimeInSeconds.ToString(CultureInfo.InvariantCulture));
            _voiceSDKLogger.LogAnnotation("maxRecordingTime",
                RuntimeConfiguration?.maxRecordingTime.ToString(CultureInfo.InvariantCulture));
        }
        #endregion

        #region Cleanup

        private void CleanupSession()
        {
            _activeSession = null;
            _activeRequestOptions = null;
            _isActive = false;
        }

        #endregion
    }
}
