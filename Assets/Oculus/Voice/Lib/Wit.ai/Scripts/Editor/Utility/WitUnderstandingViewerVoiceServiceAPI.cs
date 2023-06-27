/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.Windows
{
    [InitializeOnLoad]
    public class WitUnderstandingViewerVoiceServiceAPI : WitUnderstandingViewerServiceAPI
    {
        private VoiceService _service;

        static WitUnderstandingViewerVoiceServiceAPI()
        {
            WitUnderstandingViewerServiceApiFactory.Register("IVoiceService", CreateApiWrapper);
        }

        public WitUnderstandingViewerVoiceServiceAPI(VoiceService service) : base(service)
        {
            _service = service;

            _hasVoiceActivation = true;
            _hasTextActivation = true;
            _shouldSubmitUtterance = true;
        }

        public override bool Active
        {
            get => _service.Active;
        }

        public override bool MicActive
        {
            get => _service.MicActive;
        }

        public override bool IsRequestActive
        {
            get => _service.IsRequestActive;
        }

        public override void Activate()
        {
            _service.Activate();
        }

        public override void Activate(string text)
        {
            _service.Activate(text);
        }

        public override void Deactivate()
        {
            _service.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            _service.DeactivateAndAbortRequest();
        }

        public override VoiceServiceRequestEvent OnSend
        {
            get => _service.VoiceEvents.OnSend;
        }

        public override WitTranscriptionEvent OnPartialTranscription
        {
            get => _service.VoiceEvents.OnPartialTranscription;
        }

        public override WitTranscriptionEvent OnFullTranscription
        {
            get => _service.VoiceEvents.OnFullTranscription;
        }

        public override UnityEvent OnStoppedListening
        {
            get => _service.VoiceEvents.OnStoppedListening;
        }

        public override VoiceServiceRequestEvent OnComplete
        {
            get => _service.VoiceEvents.OnComplete;
        }

        public static WitUnderstandingViewerServiceAPI CreateApiWrapper(MonoBehaviour service)
        {
            return new WitUnderstandingViewerVoiceServiceAPI((VoiceService)service);
        }
    }
}
