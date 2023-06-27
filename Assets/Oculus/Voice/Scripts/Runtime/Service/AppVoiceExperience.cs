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
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Oculus.Voice.Bindings.Android;
using Oculus.Voice.Core.Bindings.Android.PlatformLogger;
using Oculus.Voice.Core.Bindings.Interfaces;
using Oculus.Voice.Interfaces;
using Oculus.VoiceSDK.Utilities;
using UnityEngine;

namespace Oculus.Voice
{
    [HelpURL("https://developer.oculus.com/experimental/voice-sdk/tutorial-overview/")]
    public class AppVoiceExperience : VoiceService, IWitRuntimeConfigProvider, IWitConfigurationProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;
        [Tooltip("Uses platform services to access wit.ai instead of accessing wit directly from within the application.")]
        [SerializeField] private bool usePlatformServices;

        [Tooltip("Enables logs related to the interaction to be displayed on console")]
        [SerializeField] private bool enableConsoleLogging;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        public WitConfiguration Configuration => witRuntimeConfiguration?.witConfiguration;

        private IPlatformVoiceService platformService;
        private IVoiceService voiceServiceImpl;
        private IVoiceSDKLogger voiceSDKLoggerImpl;
#if UNITY_ANDROID && !UNITY_EDITOR
        // This version is auto-updated for a release build
        private readonly string PACKAGE_VERSION = "54.0.0.135.284";
#endif

        private bool Initialized => null != voiceServiceImpl;

        public event Action OnInitialized;

        #region Voice Service Properties
        public override bool Active => base.Active || (null != voiceServiceImpl && voiceServiceImpl.Active);
        public override bool IsRequestActive => base.IsRequestActive || (null != voiceServiceImpl && voiceServiceImpl.IsRequestActive);
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => voiceServiceImpl?.TranscriptionProvider;
            set
            {
                if (voiceServiceImpl != null)
                {
                    voiceServiceImpl.TranscriptionProvider = value;
                }
            }
        }
        public override bool MicActive => null != voiceServiceImpl && voiceServiceImpl.MicActive;
        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        #if UNITY_ANDROID && !UNITY_EDITOR
        public bool HasPlatformIntegrations => usePlatformServices && voiceServiceImpl is VoiceSDKImpl;
        #else
        public bool HasPlatformIntegrations => false;
        #endif

        public bool EnableConsoleLogging => enableConsoleLogging;

        public bool UsePlatformIntegrations
        {
            get => usePlatformServices;
            set
            {
                // If we're trying to turn on platform services and they're not currently active we
                // will forcibly reinit and try to set the state.
                if (usePlatformServices != value || HasPlatformIntegrations != value)
                {
                    usePlatformServices = value;
#if UNITY_ANDROID && !UNITY_EDITOR
                    Debug.Log($"{(usePlatformServices ? "Enabling" : "Disabling")} platform integration.");
                    InitVoiceSDK();
#endif
                }
            }
        }

        #region Voice Service Text Methods
        public override bool CanSend()
        {
            return base.CanSend() && voiceServiceImpl.CanSend();
        }

        public override VoiceServiceRequest Activate(string text, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            if (CanSend())
            {
                voiceSDKLoggerImpl.LogInteractionStart(requestOptions.RequestId, "message");
                LogRequestConfig();
                return voiceServiceImpl.Activate(text, requestOptions, requestEvents);
            }
            return null;
        }
        #endregion

        #region Voice Service Audio Methods
        public override bool CanActivateAudio()
        {
            return base.CanActivateAudio() && voiceServiceImpl.CanActivateAudio();
        }

        protected override string GetActivateAudioError()
        {
            if (!HasPlatformIntegrations && !AudioBuffer.Instance.IsInputAvailable)
            {
                return "No Microphone(s)/recording devices found.  You will be unable to capture audio on this device.";
            }
            return string.Empty;
        }

        public override VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            if (CanActivateAudio() && CanSend())
            {
                voiceSDKLoggerImpl.LogInteractionStart(requestOptions.RequestId, "speech");
                LogRequestConfig();
                return voiceServiceImpl.Activate(requestOptions, requestEvents);
            }
            return null;
        }

        public override VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            if (CanActivateAudio() && CanSend())
            {
                voiceSDKLoggerImpl.LogInteractionStart(requestOptions.RequestId, "speech");
                LogRequestConfig();
                return voiceServiceImpl.ActivateImmediately(requestOptions, requestEvents);
            }
            return null;
        }

        public override void Deactivate()
        {
            voiceServiceImpl.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            voiceServiceImpl.DeactivateAndAbortRequest();
        }

        #endregion

        private void InitVoiceSDK()
        {
            // Clean up if we're switching to native C# wit impl
            if (!UsePlatformIntegrations)
            {
                if (voiceServiceImpl is VoiceSDKImpl)
                {
                    ((VoiceSDKImpl) voiceServiceImpl).Disconnect();
                }

                if (voiceSDKLoggerImpl is VoiceSDKPlatformLoggerImpl)
                {
                    try
                    {
                        ((VoiceSDKPlatformLoggerImpl)voiceSDKLoggerImpl).Disconnect();
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
            voiceSDKLoggerImpl = loggerImpl;

            if (UsePlatformIntegrations)
            {
                Debug.Log("Checking platform capabilities...");
                var platformImpl = new VoiceSDKImpl(this);
                platformImpl.OnServiceNotAvailableEvent += () => RevertToWitUnity();
                platformImpl.Connect(PACKAGE_VERSION);
                platformImpl.SetRuntimeConfiguration(RuntimeConfiguration);
                if (platformImpl.PlatformSupportsWit)
                {
                    voiceServiceImpl = platformImpl;

                    if (voiceServiceImpl is Wit wit)
                    {
                        wit.RuntimeConfiguration = witRuntimeConfiguration;
                    }

                    voiceServiceImpl.VoiceEvents = VoiceEvents;
                    voiceServiceImpl.TelemetryEvents = TelemetryEvents;
                    voiceSDKLoggerImpl.IsUsingPlatformIntegration = true;
                }
                else
                {
                    Debug.Log("Platform registration indicated platform support is not currently available.");
                    RevertToWitUnity();
                }
            }
            else
            {
                RevertToWitUnity();
            }
#else
            voiceSDKLoggerImpl = new VoiceSDKConsoleLoggerImpl();
            RevertToWitUnity();
#endif
            voiceSDKLoggerImpl.WitApplication = RuntimeConfiguration?.witConfiguration?.GetLoggerAppId();
            voiceSDKLoggerImpl.ShouldLogToConsole = EnableConsoleLogging;

            OnInitialized?.Invoke();
        }

        private void RevertToWitUnity()
        {
            Wit w = GetComponent<Wit>();
            if (null == w)
            {
                w = gameObject.AddComponent<Wit>();
                w.hideFlags = HideFlags.HideInInspector;
            }
            voiceServiceImpl = w;

            if (voiceServiceImpl is Wit wit)
            {
                wit.RuntimeConfiguration = witRuntimeConfiguration;
            }

            voiceServiceImpl.VoiceEvents = VoiceEvents;
            voiceServiceImpl.TelemetryEvents = TelemetryEvents;
            voiceSDKLoggerImpl.IsUsingPlatformIntegration = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MicPermissionsManager.HasMicPermission())
            {
                InitVoiceSDK();
            }
            else
            {
                MicPermissionsManager.RequestMicPermission();
            }

            #if UNITY_ANDROID && !UNITY_EDITOR
            platformService?.SetRuntimeConfiguration(witRuntimeConfiguration);
            #endif

            // Logging
            VoiceEvents.OnResponse?.AddListener(OnWitResponseListener);
            VoiceEvents.OnStartListening?.AddListener(OnStartedListening);
            VoiceEvents.OnMinimumWakeThresholdHit?.AddListener(OnMinimumWakeThresholdHit);
            VoiceEvents.OnStoppedListening?.AddListener(OnStoppedListening);
            VoiceEvents.OnMicDataSent?.AddListener(OnMicDataSent);
            VoiceEvents.OnSend?.AddListener(OnSend);
            VoiceEvents.OnPartialTranscription?.AddListener(OnPartialTranscription);
            VoiceEvents.OnFullTranscription?.AddListener(OnFullTranscription);
            VoiceEvents.OnStoppedListeningDueToTimeout?.AddListener(OnStoppedListeningDueToTimeout);
            VoiceEvents.OnStoppedListeningDueToInactivity?.AddListener(OnStoppedListeningDueToInactivity);
            VoiceEvents.OnStoppedListeningDueToDeactivation?.AddListener(OnStoppedListeningDueToDeactivation);
            VoiceEvents.OnComplete?.AddListener(OnRequestComplete);
            TelemetryEvents.OnAudioTrackerFinished?.AddListener(OnAudioDurationTrackerFinished);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            #if UNITY_ANDROID
            if (voiceServiceImpl is VoiceSDKImpl platformImpl)
            {
                platformImpl.Disconnect();
            }

            if (voiceSDKLoggerImpl is VoiceSDKPlatformLoggerImpl loggerImpl)
            {
                loggerImpl.Disconnect();
            }
            #endif
            voiceServiceImpl = null;
            voiceSDKLoggerImpl = null;

            // Logging
            VoiceEvents.OnResponse?.RemoveListener(OnWitResponseListener);
            VoiceEvents.OnStartListening?.RemoveListener(OnStartedListening);
            VoiceEvents.OnMinimumWakeThresholdHit?.RemoveListener(OnMinimumWakeThresholdHit);
            VoiceEvents.OnStoppedListening?.RemoveListener(OnStoppedListening);
            VoiceEvents.OnMicDataSent?.RemoveListener(OnMicDataSent);
            VoiceEvents.OnSend?.RemoveListener(OnSend);
            VoiceEvents.OnPartialTranscription?.RemoveListener(OnPartialTranscription);
            VoiceEvents.OnFullTranscription?.RemoveListener(OnFullTranscription);
            VoiceEvents.OnStoppedListeningDueToTimeout?.RemoveListener(OnStoppedListeningDueToTimeout);
            VoiceEvents.OnStoppedListeningDueToInactivity?.RemoveListener(OnStoppedListeningDueToInactivity);
            VoiceEvents.OnStoppedListeningDueToDeactivation?.RemoveListener(OnStoppedListeningDueToDeactivation);
            VoiceEvents.OnComplete?.RemoveListener(OnRequestComplete);
            TelemetryEvents.OnAudioTrackerFinished?.RemoveListener(OnAudioDurationTrackerFinished);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (enabled && hasFocus && !Initialized)
            {
                if (MicPermissionsManager.HasMicPermission())
                {
                    InitVoiceSDK();
                }
            }
        }

        #region Event listeners for logging

        void OnWitResponseListener(WitResponseNode witResponseNode)
        {
            var tokens = witResponseNode?["speech"]?["tokens"];
            if (tokens != null)
            {
                int speechTokensLength = tokens.Count;
                string speechLength = witResponseNode["speech"]["tokens"][speechTokensLength - 1]?["end"]?.Value;
                voiceSDKLoggerImpl.LogAnnotation("audioLength", speechLength);
            }
        }

        void OnStartedListening()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("startedListening");
        }

        void OnMinimumWakeThresholdHit()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("minWakeThresholdHit");
        }

        void OnStoppedListening()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("stoppedListening");
        }

        void OnStoppedListeningDueToTimeout()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("stoppedListeningTimeout");
        }

        void OnStoppedListeningDueToInactivity()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("stoppedListeningInactivity");
        }

        void OnStoppedListeningDueToDeactivation()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("stoppedListeningDeactivate");
        }

        void OnMicDataSent()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("micDataSent");
        }

        void OnSend(VoiceServiceRequest request)
        {
            voiceSDKLoggerImpl.LogInteractionPoint("witRequestCreated");
            if (request != null)
            {
                voiceSDKLoggerImpl.LogAnnotation("requestIdOverride", request.Options?.RequestId);
            }
        }

        void OnAudioDurationTrackerFinished(long timestamp, double audioDuration)
        {
            voiceSDKLoggerImpl.LogAnnotation("adt_duration", audioDuration.ToString(CultureInfo.InvariantCulture));
            voiceSDKLoggerImpl.LogAnnotation("adt_finished", timestamp.ToString());
        }

        void OnPartialTranscription(string text)
        {
            voiceSDKLoggerImpl.LogFirstTranscriptionTime();
        }

        void OnFullTranscription(string text)
        {
            voiceSDKLoggerImpl.LogInteractionPoint("fullTranscriptionTime");
        }

        void OnRequestComplete(VoiceServiceRequest request)
        {
            if (request.State == VoiceRequestState.Failed)
            {
                VLog.E($"Request Failed\nError: {request.Results.Message}");
                voiceSDKLoggerImpl.LogInteractionEndFailure(request.Results.Message);
            }
            else if (request.State == VoiceRequestState.Canceled)
            {
                VLog.W($"Request Canceled\nMessage: {request.Results.Message}");
                voiceSDKLoggerImpl.LogInteractionEndFailure("aborted");
            }
            else
            {
                VLog.D($"Request Success");
                voiceSDKLoggerImpl.LogInteractionEndSuccess();
            }
        }

        void LogRequestConfig()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            voiceSDKLoggerImpl.LogAnnotation("clientSDKVersion", PACKAGE_VERSION);
#endif
            voiceSDKLoggerImpl.LogAnnotation("minWakeThreshold",
                RuntimeConfiguration?.soundWakeThreshold.ToString(CultureInfo.InvariantCulture));
            voiceSDKLoggerImpl.LogAnnotation("minKeepAliveTimeSec",
                RuntimeConfiguration?.minKeepAliveTimeInSeconds.ToString(CultureInfo.InvariantCulture));
            voiceSDKLoggerImpl.LogAnnotation("minTranscriptionKeepAliveTimeSec",
                RuntimeConfiguration?.minTranscriptionKeepAliveTimeInSeconds.ToString(CultureInfo.InvariantCulture));
            voiceSDKLoggerImpl.LogAnnotation("maxRecordingTime",
                RuntimeConfiguration?.maxRecordingTime.ToString(CultureInfo.InvariantCulture));
        }
        #endregion
    }
}
