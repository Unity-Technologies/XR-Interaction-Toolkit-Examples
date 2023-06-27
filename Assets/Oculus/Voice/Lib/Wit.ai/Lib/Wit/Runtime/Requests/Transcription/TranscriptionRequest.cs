/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using Meta.WitAi;
using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// Abstract class for all transcription requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public abstract class TranscriptionRequest<TUnityEvent, TOptions, TEvents, TResults>
        : VoiceRequest<TUnityEvent, TOptions, TEvents, TResults>,
            ITranscriptionRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : ITranscriptionRequestOptions
        where TEvents : TranscriptionRequestEvents<TUnityEvent>
        where TResults : ITranscriptionRequestResults
    {
        /// <summary>
        /// The current audio input state
        /// </summary>
        public VoiceAudioInputState AudioInputState { get; private set; } = VoiceAudioInputState.Off;

        /// <summary>
        /// Whether or not audio is currently activated
        /// </summary>
        public bool IsAudioInputActivated => AudioInputState == VoiceAudioInputState.Activating
                                        || AudioInputState == VoiceAudioInputState.On;
        /// <summary>
        /// Whether or not audio is currently being listened to
        /// </summary>
        public bool IsListening => AudioInputState == VoiceAudioInputState.On;

        /// <summary>
        /// Determine whether audio can be activated based on activation error existing
        /// </summary>
        public bool CanActivateAudio => string.IsNullOrEmpty(GetActivateAudioError());

        /// <summary>
        /// Determine whether audio can be activated based on activation error existing
        /// </summary>
        public bool CanDeactivateAudio => IsAudioInputActivated;

        #region INITIALIZATION
        /// <summary>
        /// Constructor class for transcription requests
        /// </summary>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected TranscriptionRequest(TOptions newOptions, TEvents newEvents) : base(newOptions, newEvents) {}

        /// <summary>
        /// Set audio input state
        /// </summary>
        protected virtual void SetAudioInputState(VoiceAudioInputState newAudioInputState)
        {
            // Ignore if same
            if (AudioInputState == newAudioInputState)
            {
                return;
            }

            // Apply audio input state
            AudioInputState = newAudioInputState;
            RaiseEvent(Events?.OnAudioInputStateChange);

            // Raise events
            switch (AudioInputState)
            {
                case VoiceAudioInputState.Activating:
                    OnAudioActivation();
                    HandleAudioActivation();
                    break;
                case VoiceAudioInputState.On:
                    OnStartListening();
                    break;
                case VoiceAudioInputState.Deactivating:
                    OnAudioDeactivation();
                    HandleAudioDeactivation();
                    break;
                case VoiceAudioInputState.Off:
                    OnStopListening();
                    break;
            }
        }
        /// <summary>
        /// Append request specific data to log
        /// </summary>
        /// <param name="log">Building log</param>
        /// <param name="warning">True if this is a warning log</param>
        protected override void AppendLogData(StringBuilder log, bool warning)
        {
            base.AppendLogData(log, warning);
            // Append audio input state
            log.AppendLine($"Audio Input State: {AudioInputState}");
            // Append current transcription
            log.AppendLine($"Transcription: {Results?.Transcription}");
        }
        #endregion INITIALIZATION

        #region TRANSCRIPTION
        /// <summary>
        /// Transcription data
        /// </summary>
        public string Transcription
        {
            get => Results?.Transcription;
        }
        /// <summary>
        /// Whether the current transcription is a final transcription or not
        /// </summary>
        public bool IsFinalTranscription
        {
            get => Results != null && Results.IsFinalTranscription;
        }
        /// <summary>
        /// An array of all finalized transcriptions
        /// </summary>
        public string[] FinalTranscriptions
        {
            get => Results?.FinalTranscriptions;
        }
        /// <summary>
        /// Applies a transcription to the current results
        /// </summary>
        /// <param name="newTranscription">The transcription returned</param>
        protected abstract void ApplyTranscription(string newTranscription, bool newIsFinal);

        /// <summary>
        /// Called when transcription has been set
        /// </summary>
        protected virtual void OnTranscriptionChanged()
        {
            if (!IsFinalTranscription)
            {
                Events?.OnPartialTranscription?.Invoke(Transcription);
            }
            else
            {
                Events?.OnFullTranscription?.Invoke(Transcription);
            }
        }
        #endregion TRANSCRIPTION

        #region ACTIVATION
        /// <summary>
        /// Implementations need to provide errors when audio input is not found
        /// </summary>
        protected abstract string GetActivateAudioError();

        /// <summary>
        /// Public request to activate audio input
        /// </summary>
        public virtual void ActivateAudio()
        {
            // Ignore if already activated
            if (IsAudioInputActivated)
            {
                LogW($"Activate Audio Ignored\nReason: Already activated");
                return;
            }

            // Fail if activation is not possible
            string activationError = GetActivateAudioError();
            if (!string.IsNullOrEmpty(activationError))
            {
                LogW($"Activate Audio Failed\nReason: {activationError}");
                HandleFailure(activationError);
                return;
            }

            // Begin activating
            SetAudioInputState(VoiceAudioInputState.Activating);
        }

        /// <summary>
        /// Called when audio activation begins
        /// </summary>
        protected virtual void OnAudioActivation()
        {
            Log("Activate Audio Begin");
            RaiseEvent(Events?.OnAudioActivation);
        }

        /// <summary>
        /// Child class audio activation handler
        /// needs to call SetAudioInputState when complete
        /// </summary>
        protected abstract void HandleAudioActivation();

        /// <summary>
        /// Called when audio activation is in effect
        /// and input is being listened to
        /// </summary>
        protected virtual void OnStartListening()
        {
            Log("Activate Audio Complete");
            RaiseEvent(Events?.OnStartListening);
        }
        #endregion ACTIVATION

        #region DEACTIVATION
        /// <summary>
        /// Public request to deactivate audio input
        /// </summary>
        public virtual void DeactivateAudio()
        {
            // Ignore if not activated
            if (!IsAudioInputActivated)
            {
                LogW($"Deactivate Audio Ignored\nReason: Not currently activated");
                return;
            }

            // Set deactivation
            SetAudioInputState(VoiceAudioInputState.Deactivating);
        }

        /// <summary>
        /// Called when audio deactivation begins
        /// </summary>
        protected virtual void OnAudioDeactivation()
        {
            Log("Deactivate Audio Begin");
            RaiseEvent(Events?.OnAudioDeactivation);
        }

        /// <summary>
        /// Child class audio deactivation handler
        /// needs to call SetAudioInputState when complete
        /// </summary>
        protected abstract void HandleAudioDeactivation();

        /// <summary>
        /// Called when audio input state is no longer
        /// being listened to
        /// </summary>
        protected virtual void OnStopListening()
        {
            Log("Deactivate Audio Complete");
            RaiseEvent(Events?.OnStopListening);

            // Handle early cancellation
            if (State == VoiceRequestState.Initialized)
            {
                Cancel(WitConstants.CANCEL_MESSAGE_PRE_SEND);
            }
        }
        #endregion DEACTIVATION

        #region TRANSMISSION
        /// <summary>
        /// Ensure audio is activated prior to sending
        /// </summary>
        public override void Send()
        {
            // Activate audio if needed & possible
            if (!IsAudioInputActivated && CanActivateAudio && CanSend)
            {
                ActivateAudio();
            }

            // Send audio request if possible
            base.Send();
        }
        /// <summary>
        /// Method for subclass success handling
        /// </summary>
        protected override void OnSuccess()
        {
            // Handle transcription as though it is final
            if (!IsFinalTranscription && !string.IsNullOrEmpty(Transcription))
            {
                ApplyTranscription(Transcription, true);
            }
            // Call success events
            base.OnSuccess();
        }
        #endregion

        #region CANCELLATION
        /// <summary>
        /// Ensure audio is deactivated prior to cancellation
        /// </summary>
        public override void Cancel(string reason = WitConstants.CANCEL_MESSAGE_DEFAULT)
        {
            // Deactivate audio if needed
            if (IsAudioInputActivated)
            {
                DeactivateAudio();
            }

            // Cancel audio request
            base.Cancel(reason);
        }
        #endregion
    }
}
