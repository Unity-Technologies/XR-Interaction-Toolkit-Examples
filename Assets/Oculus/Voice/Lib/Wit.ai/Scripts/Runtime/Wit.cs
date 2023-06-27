/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using UnityEngine;

namespace Meta.WitAi
{
    public class Wit : VoiceService, IWitRuntimeConfigProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        private WitService witService;

        #region Voice Service Properties
        public override bool Active => base.Active || (null != witService && witService.Active);
        public override bool IsRequestActive => base.IsRequestActive || (null != witService && witService.IsRequestActive);
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => witService.TranscriptionProvider;
            set => witService.TranscriptionProvider = value;
        }
        public override bool MicActive => null != witService && witService.MicActive;
        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        #region Voice Service Methods

        protected override string GetSendError()
        {
            if (!RuntimeConfiguration?.witConfiguration)
            {
                return $"Your {GetType().Name} \"{gameObject.name}\" does not have a wit configuration assigned.   Voice interactions are not possible without the configuration.";
            }
            if (string.IsNullOrEmpty(RuntimeConfiguration.witConfiguration.GetClientAccessToken()))
            {
                return $"The configuration \"{RuntimeConfiguration.witConfiguration.name}\" is not setup with a valid client access token.   Voice interactions are not possible without the token.";
            }
            return base.GetSendError();
        }

        protected override string GetActivateAudioError()
        {
            if (!AudioBuffer.Instance.IsInputAvailable)
            {
                return "No Microphone(s)/recording devices found.  You will be unable to capture audio on this device.";
            }
            return string.Empty;
        }

        public override VoiceServiceRequest Activate(string text, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            VoiceServiceRequest request = witService.Activate(text, requestOptions, requestEvents);
            OnTextRequestCreated(request);
            return request;
        }

        public override VoiceServiceRequest Activate(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            VoiceServiceRequest request = witService.Activate(requestOptions, requestEvents);
            OnAudioRequestCreated(request);
            return request;
        }

        public override VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents)
        {
            VoiceServiceRequest request = witService.ActivateImmediately(requestOptions, requestEvents);
            OnAudioRequestCreated(request);
            return request;
        }

        public override void Deactivate()
        {
            witService.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            witService.DeactivateAndAbortRequest();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();

            // WitService is 1:1 tied to a VoiceService. In the event there
            // are multiple voice services on a game object this will ensure
            // that this component has its own dedicated WitService
            witService = gameObject.AddComponent<WitService>();
            witService.VoiceEventProvider = this;
            witService.TelemetryEventsProvider = this;
            witService.ConfigurationProvider = this;
        }
    }
}
