/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Voice;
using Meta.WitAi.Json;
using UnityEngine.Events;

namespace Meta.WitAi.Requests
{
    /// <summary>
    /// A set of events associated with a Voice Service activation.
    /// </summary>
    [Serializable]
    public class VoiceServiceRequestEvents
        : NLPRequestEvents<VoiceServiceRequestEvent>
    {

    }
    
    /// <summary>
    /// A UnityEvent with a parameter of VoiceServiceRequest
    /// </summary>
    [Serializable]
    public class VoiceServiceRequestEvent : UnityEvent<VoiceServiceRequest> {}


    /// <summary>
    /// A base class to provide quick overridable methods that map to events from VoiceServiceRequestEvents.
    /// </summary>
    public class VoiceServiceRequestEventsWrapper
    {
        /// <summary>
        /// Adds all listeners for VoiceServiceRequestEvents to overridable methods
        /// </summary>
        /// <param name="events"></param>
        public void Wrap(VoiceServiceRequestEvents events)
        {
            events.OnCancel.AddListener(OnCancel);
            events.OnComplete.AddListener(OnComplete);
            events.OnFailed.AddListener(OnFailed);
            events.OnInit.AddListener(OnInit);
            events.OnSend.AddListener(OnSend);
            events.OnSuccess.AddListener(OnSuccess);
            events.OnAudioActivation.AddListener(OnAudioActivation);
            events.OnAudioDeactivation.AddListener(OnAudioDeactivation);
            events.OnFullResponse.AddListener(OnFullResponse);
            events.OnPartialResponse.AddListener(OnPartialResponse);
            events.OnPartialTranscription.AddListener(OnPartialTranscription);
            events.OnFullTranscription.AddListener(OnFullTranscription);
            events.OnStartListening.AddListener(OnStartListening);
            events.OnStopListening.AddListener(OnStopListening);
            events.OnStateChange.AddListener(OnStateChange);
            events.OnDownloadProgressChange.AddListener(OnDownloadProgressChange);
            events.OnUploadProgressChange.AddListener(OnUploadProgressChange);
            events.OnAudioInputStateChange.AddListener(OnAudioInputStateChange);
        }
        
        /// <summary>
        /// Removes all listeners for the provided VoiceServiceRequestEvents event object.
        /// </summary>
        /// <param name="events"></param>
        public void Unwrap(VoiceServiceRequestEvents events)
        {
            events.OnCancel.RemoveListener(OnCancel);
            events.OnComplete.RemoveListener(OnComplete);
            events.OnFailed.RemoveListener(OnFailed);
            events.OnInit.RemoveListener(OnInit);
            events.OnSend.RemoveListener(OnSend);
            events.OnSuccess.RemoveListener(OnSuccess);
            events.OnAudioActivation.RemoveListener(OnAudioActivation);
            events.OnAudioDeactivation.RemoveListener(OnAudioDeactivation);
            events.OnFullResponse.RemoveListener(OnFullResponse);
            events.OnPartialResponse.RemoveListener(OnPartialResponse);
            events.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            events.OnFullTranscription.RemoveListener(OnFullTranscription);
            events.OnStartListening.RemoveListener(OnStartListening);
            events.OnStopListening.RemoveListener(OnStopListening);
            events.OnStateChange.RemoveListener(OnStateChange);
            events.OnDownloadProgressChange.RemoveListener(OnDownloadProgressChange);
            events.OnUploadProgressChange.RemoveListener(OnUploadProgressChange);
            events.OnAudioInputStateChange.RemoveListener(OnAudioInputStateChange);
        }

        protected virtual void OnAudioInputStateChange(VoiceServiceRequest request)
        {
        }

        protected virtual void OnUploadProgressChange(VoiceServiceRequest request)
        {
        }

        protected virtual void OnDownloadProgressChange(VoiceServiceRequest request)
        {
        }

        protected virtual void OnStateChange(VoiceServiceRequest request)
        {
        }

        protected virtual void OnStopListening(VoiceServiceRequest request)
        {
        }

        protected virtual void OnStartListening(VoiceServiceRequest request)
        {
        }

        protected virtual void OnFullTranscription(string transcription)
        {
        }

        protected virtual void OnPartialTranscription(string transcription)
        {
        }

        protected virtual void OnPartialResponse(WitResponseNode request)
        {
        }

        protected virtual void OnFullResponse(WitResponseNode request)
        {
        }

        protected virtual void OnAudioDeactivation(VoiceServiceRequest request)
        {
        }

        protected virtual void OnAudioActivation(VoiceServiceRequest request)
        {
        }

        protected virtual void OnSuccess(VoiceServiceRequest request)
        {
        }

        protected virtual void OnSend(VoiceServiceRequest request)
        {
        }

        protected virtual void OnInit(VoiceServiceRequest request)
        {
        }

        protected virtual void OnFailed(VoiceServiceRequest request)
        {
        }

        protected virtual void OnComplete(VoiceServiceRequest request)
        {
            
        }

        protected virtual void OnCancel(VoiceServiceRequest request)
        {
            
        }
    }
}
