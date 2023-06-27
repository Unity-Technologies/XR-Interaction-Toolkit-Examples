/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Speech;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi.TTS.Data;

namespace Meta.WitAi.TTS.Utilities
{
    [Serializable]
    public class TTSSpeakerEvent : UnityEvent<TTSSpeaker, string> { }
    [Serializable]
    public class TTSSpeakerClipDataEvent : UnityEvent<TTSClipData> { }
    [Serializable]
    public class TTSSpeakerEvents : VoiceSpeechEvents
    {
        [Header("Speaker Events")]
        [Tooltip("Called when a speaking begins")]
        public TTSSpeakerEvent OnStartSpeaking;
        [Tooltip("Called when a speaking finishes")]
        public TTSSpeakerEvent OnFinishedSpeaking;
        [Tooltip("Called when a speaking is cancelled")]
        public TTSSpeakerEvent OnCancelledSpeaking;
        [Tooltip("Called when TTS audio clip load begins")]
        public TTSSpeakerEvent OnClipLoadBegin;
        [Tooltip("Called when TTS audio clip load fails")]
        public TTSSpeakerEvent OnClipLoadFailed;
        [Tooltip("Called when TTS audio clip load successfully")]
        public TTSSpeakerEvent OnClipLoadSuccess;
        [Tooltip("Called when TTS audio clip load is cancelled")]
        public TTSSpeakerEvent OnClipLoadAbort;

        [Header("TTSClip Data Events")]
        [Tooltip("Called when a new clip is added to the playback queue")]
        public TTSSpeakerClipDataEvent OnClipDataQueued;
        [Tooltip("Called when TTS audio clip load begins")]
        public TTSSpeakerClipDataEvent OnClipDataLoadBegin;
        [Tooltip("Called when TTS audio clip load fails")]
        public TTSSpeakerClipDataEvent OnClipDataLoadFailed;
        [Tooltip("Called when TTS audio clip load successfully")]
        public TTSSpeakerClipDataEvent OnClipDataLoadSuccess;
        [Tooltip("Called when TTS audio clip load is cancelled")]
        public TTSSpeakerClipDataEvent OnClipDataLoadAbort;
        [Tooltip("Called when a clip is ready for playback")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackReady;
        [Tooltip("Called when a clip playback has begun")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackStart;
        [Tooltip("Called when a clip playback has completed successfully")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackFinished;
        [Tooltip("Called when a clip playback has been cancelled")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackCancelled;

        [Header("Queue Events")]
        [Tooltip("Called when a tts request is added to an empty queue")]
        public UnityEvent OnPlaybackQueueBegin;
        [Tooltip("Called the final request is removed from a queue")]
        public UnityEvent OnPlaybackQueueComplete;
    }
}
