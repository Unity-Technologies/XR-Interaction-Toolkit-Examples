/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// Interface for audio transcription based voice requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public interface ITranscriptionRequest<TUnityEvent, TOptions, TEvents, TResults>
        : IVoiceRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : ITranscriptionRequestOptions
        where TEvents : ITranscriptionRequestEvents<TUnityEvent>
        where TResults : ITranscriptionRequestResults
    {
        /// <summary>
        /// The current audio input state
        /// </summary>
        VoiceAudioInputState AudioInputState { get; }
        /// <summary>
        /// Whether audio input has been requested or is currently on
        /// </summary>
        bool IsAudioInputActivated { get; }
        /// <summary>
        /// Whether audio input is being listened to or not
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Whether audio input can currently be activated for this request
        /// </summary>
        bool CanActivateAudio { get; }
        /// <summary>
        /// Activate the listening of audio.  Should automatically be called via the Send method as well
        /// </summary>
        void ActivateAudio();

        /// <summary>
        /// Whether audio input can currently be deactivated for this request
        /// </summary>
        bool CanDeactivateAudio { get; }
        /// <summary>
        /// Deactivate the listening of audio.
        /// </summary>
        void DeactivateAudio();
    }
}
