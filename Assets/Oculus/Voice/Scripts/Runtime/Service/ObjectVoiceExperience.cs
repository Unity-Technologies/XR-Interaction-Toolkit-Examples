/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Configuration;
using Meta.WitAi.Events;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Voice
{
    public class ObjectVoiceExperience : MonoBehaviour
    {
        // TODO: T149927662 Clean FormerlySerializedAs in a follow up diff before release.
        [FormerlySerializedAs("voiceEvents")]
        [SerializeField] private VoiceEvents _voiceEvents = new VoiceEvents();

        private AppVoiceExperience _voice;
        private VoiceServiceRequest _activation;
        private VoiceServiceRequestEvents _events = new VoiceServiceRequestEvents();

        private void OnEnable()
        {
            if (!_voice) _voice = FindObjectOfType<AppVoiceExperience>();

            _events.OnCancel.AddListener(HandleCancel);
            _events.OnComplete.AddListener(HandleComplete);
            _events.OnFailed.AddListener(HandleFailed);
            _events.OnInit.AddListener(HandleInit);
            _events.OnSend.AddListener(HandleSend);
            _events.OnSuccess.AddListener(HandleSuccess);
            _events.OnAudioActivation.AddListener(HandleAudioActivation);
            _events.OnAudioDeactivation.AddListener(HandleAudioDeactivation);
            _events.OnPartialTranscription.AddListener(HandlePartialTranscription);
            _events.OnFullTranscription.AddListener(HandleFullTranscription);
            _events.OnStateChange.AddListener(HandleStateChange);
            _events.OnStartListening.AddListener(HandleStartListening);
            _events.OnStopListening.AddListener(HandleStopListening);
            _events.OnDownloadProgressChange.AddListener(HandleDownloadProgressChange);
            _events.OnUploadProgressChange.AddListener(HandleUploadProgressChange);
            _events.OnAudioInputStateChange.AddListener(HandleAudioInputStateChange);
        }

        private void OnDisable()
        {
            _events.OnCancel.RemoveListener(HandleCancel);
            _events.OnComplete.RemoveListener(HandleComplete);
            _events.OnFailed.RemoveListener(HandleFailed);
            _events.OnInit.RemoveListener(HandleInit);
            _events.OnSend.RemoveListener(HandleSend);
            _events.OnSuccess.RemoveListener(HandleSuccess);
            _events.OnAudioActivation.RemoveListener(HandleAudioActivation);
            _events.OnAudioDeactivation.RemoveListener(HandleAudioDeactivation);
            _events.OnFullTranscription.RemoveListener(HandleFullTranscription);
            _events.OnPartialTranscription.RemoveListener(HandlePartialTranscription);
            _events.OnStateChange.RemoveListener(HandleStateChange);
            _events.OnStartListening.RemoveListener(HandleStartListening);
            _events.OnStopListening.RemoveListener(HandleStopListening);
            _events.OnDownloadProgressChange.RemoveListener(HandleDownloadProgressChange);
            _events.OnUploadProgressChange.RemoveListener(HandleUploadProgressChange);
            _events.OnAudioInputStateChange.RemoveListener(HandleAudioInputStateChange);
        }

        private void HandleAudioInputStateChange(VoiceServiceRequest request)
        {
            SendMessage("OnAudioInputStateChange", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleUploadProgressChange(VoiceServiceRequest request)
        {
            SendMessage("OnUploadProgressChange", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleDownloadProgressChange(VoiceServiceRequest request)
        {
            SendMessage("OnDownloadProgressChange", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleStopListening(VoiceServiceRequest request)
        {
            SendMessage("OnStopListening", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleStartListening(VoiceServiceRequest request)
        {
            SendMessage("OnStartListening", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleStateChange(VoiceServiceRequest request)
        {
            SendMessage("OnStateChange", request, SendMessageOptions.DontRequireReceiver);
        }
        
        private void HandleFullTranscription(string transcription)
        {
            SendMessage("OnFullTranscription", transcription, SendMessageOptions.DontRequireReceiver);
        }

        private void HandlePartialTranscription(string transcription)
        {
            SendMessage("OnPartialTranscription", transcription, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleAudioDeactivation(VoiceServiceRequest request)
        {
            SendMessage("OnAudioDeactivation", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleAudioActivation(VoiceServiceRequest request)
        {
            SendMessage("OnAudioActivation", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleSuccess(VoiceServiceRequest request)
        {
            SendMessage("OnSuccess", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleSend(VoiceServiceRequest request)
        {
            SendMessage("OnSend", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleInit(VoiceServiceRequest request)
        {
            SendMessage("OnInit", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleFailed(VoiceServiceRequest request)
        {
            SendMessage("OnFailed", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleComplete(VoiceServiceRequest request)
        {
            _voiceEvents?.OnComplete?.Invoke(request);
            SendMessage("OnComplete", request, SendMessageOptions.DontRequireReceiver);
        }

        private void HandleCancel(VoiceServiceRequest request)
        {
            _voiceEvents?.OnCanceled?.Invoke("");
            SendMessage("OnCancel", request, SendMessageOptions.DontRequireReceiver);
        }

        public void Activate()
        {
            _activation = _voice.Activate(new WitRequestOptions(), _events);
        }

        public void Deactivate()
        {
            _activation.Cancel();
        }
    }
}
