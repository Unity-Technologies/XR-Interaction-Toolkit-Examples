/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Meta.WitAi.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;

namespace Meta.WitAi.Events
{
    // A class for tracking events used for speech
    [Serializable]
    public class SpeechEvents : EventRegistry, ISpeechEvents, ITranscriptionEvent, IAudioInputEvents
    {
        #region Activation - Setup Events
        protected const string EVENT_CATEGORY_ACTIVATION_SETUP = "Activation Setup Events";

        [EventCategory(EVENT_CATEGORY_ACTIVATION_SETUP)]
        [Tooltip("Called prior to initialization for WitRequestOption customization")]
        [FormerlySerializedAs("OnRequestOptionSetup")] [SerializeField]
        private WitRequestOptionsEvent _onRequestOptionSetup = new WitRequestOptionsEvent();
        public WitRequestOptionsEvent OnRequestOptionSetup => _onRequestOptionSetup;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_SETUP)]
        [Tooltip("Called when a request is created.  This occurs as soon as a activation is called successfully.")]
        [FormerlySerializedAs("OnRequestInitialized")] [SerializeField]
        private VoiceServiceRequestEvent _onRequestInitialized = new VoiceServiceRequestEvent();
        public VoiceServiceRequestEvent OnRequestInitialized => _onRequestInitialized;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_SETUP)]
        [Tooltip("Called when a request is sent. This occurs immediately once data is being transmitted to the endpoint.")]
        [FormerlySerializedAs("OnRequestCreated")] [SerializeField] [HideInInspector]
        private WitRequestCreatedEvent _onRequestCreated = new WitRequestCreatedEvent();
        [Obsolete("Deprecated for 'OnSend' event")]
        public WitRequestCreatedEvent OnRequestCreated => _onRequestCreated;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_SETUP)]
        [Tooltip("Called when a request is sent. This occurs immediately once data is being transmitted to the endpoint.")]
        [SerializeField]
        private VoiceServiceRequestEvent _onSend = new VoiceServiceRequestEvent();
        public VoiceServiceRequestEvent OnSend => _onSend;
        #endregion Activation - Setup Events

        #region Activation - Info Events
        protected const string EVENT_CATEGORY_ACTIVATION_INFO = "Activation Info Events";

        [EventCategory(EVENT_CATEGORY_ACTIVATION_INFO)]
        [Tooltip("Fired when the minimum wake threshold is hit after an activation.  Not called for ActivateImmediately")]
        [FormerlySerializedAs("OnMinimumWakeThresholdHit")] [SerializeField]
        private UnityEvent _onMinimumWakeThresholdHit = new UnityEvent();
        public UnityEvent OnMinimumWakeThresholdHit => _onMinimumWakeThresholdHit;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_INFO)]
        [Tooltip("Fired when recording stops, the minimum volume threshold was hit, and data is being sent to the server.")]
        [FormerlySerializedAs("OnMicDataSent")] [SerializeField]
        private UnityEvent _onMicDataSent = new UnityEvent();
        public UnityEvent OnMicDataSent => _onMicDataSent;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_INFO)]
        [Tooltip("The Deactivate() method has been called ending the current activation.")]
        [FormerlySerializedAs("OnStoppedListeningDueToDeactivation")] [SerializeField]
        private UnityEvent _onStoppedListeningDueToDeactivation = new UnityEvent();
        public UnityEvent OnStoppedListeningDueToDeactivation => _onStoppedListeningDueToDeactivation;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_INFO)]
        [Tooltip("Called when the microphone input volume has been below the volume threshold for the specified duration and microphone data is no longer being collected")]
        [FormerlySerializedAs("OnStoppedListeningDueToInactivity")] [SerializeField]
        private UnityEvent _onStoppedListeningDueToInactivity = new UnityEvent();
        public UnityEvent OnStoppedListeningDueToInactivity => _onStoppedListeningDueToInactivity;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_INFO)]
        [Tooltip("The microphone has stopped recording because maximum recording time has been hit for this activation")]
        [FormerlySerializedAs("OnStoppedListeningDueToTimeout")] [SerializeField]
        private UnityEvent _onStoppedListeningDueToTimeout = new UnityEvent();
        public UnityEvent OnStoppedListeningDueToTimeout => _onStoppedListeningDueToTimeout;
        #endregion Activation - Info Events

        #region Activation - Cancelation Events
        protected const string EVENT_CATEGORY_ACTIVATION_CANCELATION = "Activation Cancelation Events";

        [EventCategory(EVENT_CATEGORY_ACTIVATION_CANCELATION)]
        [Tooltip("Called when the activation is about to be aborted by a direct user interaction via DeactivateAndAbort.")]
        [FormerlySerializedAs("OnAborting")] [SerializeField]
        private UnityEvent _onAborting = new UnityEvent();
        public UnityEvent OnAborting => _onAborting;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_CANCELATION)]
        [Tooltip("Called when the activation stopped because the network request was aborted. This can be via a timeout or call to DeactivateAndAbort.")]
        [FormerlySerializedAs("OnAborted")] [SerializeField]
        private UnityEvent _onAborted = new UnityEvent();
        public UnityEvent OnAborted => _onAborted;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_CANCELATION)]
        [Tooltip("Called when a request has been canceled either prior to or after a request has begun transmission.  Returns the cancelation reason.")]
        [FormerlySerializedAs("OnCanceled")] [SerializeField]
        private WitTranscriptionEvent _onCanceled = new WitTranscriptionEvent();
        public WitTranscriptionEvent OnCanceled => _onCanceled;
        #endregion Activation - Cancelation Events

        #region Activation - Response Events
        protected const string EVENT_CATEGORY_ACTIVATION_RESPONSE = "Activation Response Events";

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called when response from Wit.ai has been received from partial transcription")]
        [FormerlySerializedAs("OnPartialResponse")] [SerializeField] [HideInInspector]
        private WitResponseEvent _onPartialResponse = new WitResponseEvent();
        public WitResponseEvent OnPartialResponse => _onPartialResponse;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called when a response from Wit.ai has been received")]
        [FormerlySerializedAs("OnResponse")] [FormerlySerializedAs("onResponse")] [SerializeField]
        private WitResponseEvent _onResponse = new WitResponseEvent();
        public WitResponseEvent OnResponse => _onResponse;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called when there was an error with a WitRequest  or the RuntimeConfiguration is not properly configured.")]
        [FormerlySerializedAs("OnError")] [FormerlySerializedAs("onError")] [SerializeField]
        private WitErrorEvent _onError = new WitErrorEvent();
        public WitErrorEvent OnError => _onError;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called when a request has completed and all response and error callbacks have fired.  This is not called if the request was aborted.")]
        [FormerlySerializedAs("OnRequestCompleted")] [SerializeField]
        private UnityEvent _onRequestCompleted = new UnityEvent();
        public UnityEvent OnRequestCompleted => _onRequestCompleted;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called when a request has been canceled, failed, or successfully completed")]
        [FormerlySerializedAs("OnComplete")] [SerializeField]
        private VoiceServiceRequestEvent _onComplete = new VoiceServiceRequestEvent();
        public VoiceServiceRequestEvent OnComplete => _onComplete;
        #endregion Activation - Response Events

        #region Audio Events
        protected const string EVENT_CATEGORY_AUDIO_EVENTS = "Audio Events";

        [EventCategory(EVENT_CATEGORY_AUDIO_EVENTS)]
        [Tooltip("Called when the microphone has started collecting data collecting data to be sent to Wit.ai. There may be some buffering before data transmission starts.")]
        [FormerlySerializedAs("OnStartListening")] [FormerlySerializedAs("onStart")] [SerializeField]
        private UnityEvent _onStartListening = new UnityEvent();
        public UnityEvent OnStartListening => _onStartListening;
        public UnityEvent OnMicStartedListening => OnStartListening;

        [EventCategory(EVENT_CATEGORY_AUDIO_EVENTS)]
        [Tooltip("Called when the voice service is no longer collecting data from the microphone")]
        [FormerlySerializedAs("OnStoppedListening")] [FormerlySerializedAs("onStopped")] [SerializeField]
        private UnityEvent _onStoppedListening = new UnityEvent();
        public UnityEvent OnStoppedListening => _onStoppedListening;
        public UnityEvent OnMicStoppedListening => OnStoppedListening;

        [EventCategory(EVENT_CATEGORY_AUDIO_EVENTS)]
        [Tooltip("Called when the volume level of the mic input has changed")]
        [FormerlySerializedAs("OnMicLevelChanged")] [SerializeField]
        private WitMicLevelChangedEvent _onMicLevelChanged = new WitMicLevelChangedEvent();
        public WitMicLevelChangedEvent OnMicLevelChanged => _onMicLevelChanged;
        public WitMicLevelChangedEvent OnMicAudioLevelChanged => OnMicLevelChanged;
        #endregion Audio Events

        #region Transcription Events
        protected const string EVENT_CATEGORY_TRANSCRIPTION_EVENTS = "Transcription Events";

        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [Tooltip("Message fired when a partial transcription has been received.")]
        [FormerlySerializedAs("onPartialTranscription")] [FormerlySerializedAs("OnPartialTranscription")] [SerializeField]
        private WitTranscriptionEvent _onPartialTranscription = new WitTranscriptionEvent();
        public WitTranscriptionEvent OnPartialTranscription => _onPartialTranscription;
        [Obsolete("Deprecated for 'OnPartialTranscription' event")]
        public WitTranscriptionEvent onPartialTranscription => OnPartialTranscription;

        [FormerlySerializedAs("OnFullTranscription")]
        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [Tooltip("Message received when a complete transcription is received.")]
        [FormerlySerializedAs("onFullTranscription")] [FormerlySerializedAs("OnFullTranscription")] [SerializeField]
        private WitTranscriptionEvent _onFullTranscription = new WitTranscriptionEvent();
        public WitTranscriptionEvent OnFullTranscription => _onFullTranscription;
        [Obsolete("Deprecated for 'OnPartialTranscription' event")]
        public WitTranscriptionEvent onFullTranscription => OnFullTranscription;
        #endregion Transcription Events

        #region Listen Wrapping
        // Listeners
        private HashSet<SpeechEvents> _listeners = new HashSet<SpeechEvents>();

        // Adds all listener events
        public void AddListener(SpeechEvents listener)
        {
            // Ignore if null or already set
            if (listener == null || _listeners.Contains(listener))
            {
                return;
            }

            // Add all events
            if (_listeners.Count == 0)
            {
                SetEvents(true);
            }

            // Add listener
            _listeners.Add(listener);
        }
        // Removes all listener events
        public void RemoveListener(SpeechEvents listener)
        {
            // Ignore if null or not already set
            if (listener == null || !_listeners.Contains(listener))
            {
                return;
            }

            // Remove listener
            _listeners.Remove(listener);

            // Remove all events
            if (_listeners.Count == 0)
            {
                SetEvents(false);
            }
        }
        // Set events
        protected virtual void SetEvents(bool add)
        {
            SetEvent((events) => events?._onRequestOptionSetup, add);
            SetEvent((events) => events?._onRequestInitialized, add);
            SetEvent((events) => events?._onRequestCreated, add);
            SetEvent((events) => events?._onSend, add);
            SetEvent((events) => events?._onMinimumWakeThresholdHit, add);
            SetEvent((events) => events?._onMicDataSent, add);
            SetEvent((events) => events?._onStoppedListeningDueToDeactivation, add);
            SetEvent((events) => events?._onStoppedListeningDueToInactivity, add);
            SetEvent((events) => events?._onAborting, add);
            SetEvent((events) => events?._onAborted, add);
            SetEvent((events) => events?._onCanceled, add);
            SetEvent((events) => events?._onPartialResponse, add);
            SetEvent((events) => events?._onResponse, add);
            SetEvent((events) => events?._onError, add);
            SetEvent((events) => events?._onRequestCompleted, add);
            SetEvent((events) => events?._onComplete, add);
            SetEvent((events) => events?._onStartListening, add);
            SetEvent((events) => events?._onStoppedListening, add);
            SetEvent((events) => events?._onMicLevelChanged, add);
            SetEvent((events) => events?._onPartialTranscription, add);
            SetEvent((events) => events?._onFullTranscription, add);
        }
        // Set UnityEvent with no parameter
        protected void SetEvent(Func<SpeechEvents, UnityEvent> getEvent, bool add)
        {
            // Get source event
            UnityEvent sourceEvent = getEvent(this);

            // Add event
            if (!add)
            {
                sourceEvent?.RemoveAllListeners();
                return;
            }

            // Add listener
            sourceEvent?.AddListener(() =>
            {
                foreach (var listener in _listeners)
                {
                    getEvent(listener)?.Invoke();
                }
            });
        }
        // Set UnityEvent with parameter
        protected void SetEvent<T>(Func<SpeechEvents, UnityEvent<T>> getEvent, bool add)
        {
            // Get source event
            UnityEvent<T> sourceEvent = getEvent(this);

            // Add event
            if (!add)
            {
                sourceEvent?.RemoveAllListeners();
                return;
            }

            // Add listener
            sourceEvent?.AddListener((param) =>
            {
                foreach (var listener in _listeners)
                {
                    getEvent(listener)?.Invoke(param);
                }
            });
        }
        // Set UnityEvent with 2 parameters
        protected void SetEvent<T, U>(Func<SpeechEvents, UnityEvent<T, U>> getEvent, bool add)
        {
            // Get source event
            UnityEvent<T, U> sourceEvent = getEvent(this);

            // Add event
            if (!add)
            {
                sourceEvent?.RemoveAllListeners();
                return;
            }

            // Add listener
            sourceEvent?.AddListener((param1, param2) =>
            {
                foreach (var listener in _listeners)
                {
                    getEvent(listener)?.Invoke(param1, param2);
                }
            });
        }
        #endregion Listen Wrapping
    }
}
