/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.Speech
{
    [Serializable]
    public class VoiceTextEvent : UnityEvent<string> { }
    [Serializable]
    public class VoiceAudioEvent : UnityEvent<AudioClip> { }
    [Serializable]
    public class VoiceSpeechEvents
    {
        [Header("Text Events")]
        [Tooltip("Called when speech begins with the provided phrase")]
        public VoiceTextEvent OnTextPlaybackStart;
        [Tooltip("Called when speech playback is cancelled")]
        public VoiceTextEvent OnTextPlaybackCancelled;
        [Tooltip("Called when speech playback completes successfully")]
        public VoiceTextEvent OnTextPlaybackFinished;

        [Header("Audio Clip Events")]
        [Tooltip("Called when a clip is ready for playback")]
        public VoiceAudioEvent OnAudioClipPlaybackReady;
        [Tooltip("Called when a clip playback has begun")]
        public VoiceAudioEvent OnAudioClipPlaybackStart;
        [Tooltip("Called when a clip playback has been cancelled")]
        public VoiceAudioEvent OnAudioClipPlaybackCancelled;
        [Tooltip("Called when a clip playback has completed successfully")]
        public VoiceAudioEvent OnAudioClipPlaybackFinished;
    }
}
