/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine.Events;

namespace Meta.Voice
{
    [Serializable]
    public class TranscriptionRequestEvent : UnityEvent<string> {}

    /// <summary>
    /// Interface for all specific audio transcription request events
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of unity event to be called for all events</typeparam>
    public interface ITranscriptionRequestEvents<TUnityEvent>
        : IVoiceRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {
        /// <summary>
        /// Called when audio input state has changed
        /// </summary>
        TUnityEvent OnAudioInputStateChange { get; }
        /// <summary>
        /// Called when audio is activated for this request
        /// </summary>
        TUnityEvent OnAudioActivation { get; }
        /// <summary>
        /// Called when audio activation has completed and audio is being
        /// listened to for this request
        /// </summary>
        TUnityEvent OnStartListening { get; }
        /// <summary>
        /// Called when audio deactivation has been performed for this request
        /// </summary>
        TUnityEvent OnAudioDeactivation { get; }
        /// <summary>
        /// Called when audio has deactivated and is no longer being listened to
        /// for this request
        /// </summary>
        TUnityEvent OnStopListening { get; }

        /// <summary>
        /// Called on request transcription while audio is still being analyzed
        /// </summary>
        TranscriptionRequestEvent OnPartialTranscription { get; }
        /// <summary>
        /// Called on request transcription when audio has been completely transferred
        /// </summary>
        TranscriptionRequestEvent OnFullTranscription { get; }
    }
}
