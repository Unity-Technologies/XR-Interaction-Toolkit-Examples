/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Meta.WitAi.Dictation.Events
{
    [Serializable]
    public class DictationEvents : SpeechEvents
    {
        private const string EVENT_CATEGORY_DICTATION_EVENTS = "Dictation Events";

        /// <summary>
        /// Called when an individual dictation session has started. This can include multiple server activations if
        /// dictation is set up to automatically reactivate when the server endpoints an utterance.
        /// </summary>
        [Tooltip("Called when an individual dictation session has started. This can include multiple server activations if dictation is set up to automatically reactivate when the server endpoints an utterance.")]
        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        [FormerlySerializedAs("onDictationSessionStarted")] [SerializeField] [HideInInspector]
        private DictationSessionEvent _onDictationSessionStarted = new DictationSessionEvent();
        public DictationSessionEvent OnDictationSessionStarted => _onDictationSessionStarted;

        /// <summary>
        /// Called when a dictation is completed after Deactivate has been called or auto-reactivate is disabled.
        /// </summary>
        [Tooltip("Called when a dictation is completed after Deactivate has been called or auto-reactivate is disabled.")]
        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        [FormerlySerializedAs("onDictationSessionStopped")] [SerializeField] [HideInInspector]
        private DictationSessionEvent _onDictationSessionStopped = new DictationSessionEvent();
        public DictationSessionEvent OnDictationSessionStopped => _onDictationSessionStopped;

        // Deprecated events
        [Obsolete("Deprecated for 'OnDictationSessionStarted' event")]
        public DictationSessionEvent onDictationSessionStarted => OnDictationSessionStarted;
        [Obsolete("Deprecated for 'OnDictationSessionStopped' event")]
        public DictationSessionEvent onDictationSessionStopped => OnDictationSessionStopped;
        [Obsolete("Deprecated for 'OnStartListening' event")]
        public UnityEvent onStart => OnStartListening;
        [Obsolete("Deprecated for 'OnStoppedListening' event")]
        public UnityEvent onStopped => OnStoppedListening;
        [Obsolete("Deprecated for 'OnMicLevelChanged' event")]
        public WitMicLevelChangedEvent onMicAudioLevel => OnMicLevelChanged;
        [Obsolete("Deprecated for 'OnError' event")]
        public WitErrorEvent onError => OnError;
        [Obsolete("Deprecated for 'OnResponse' event")]
        public WitResponseEvent onResponse => OnResponse;
    }
}
