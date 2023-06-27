/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Events;
using Meta.WitAi.Events.UnityEventListeners;
using Meta.WitAi.Interfaces;
using UnityEngine.Events;

namespace Meta.WitAi.ServiceReferences
{
    /// <summary>
    /// Finds all audio event listeners in the scene and subscribes to them.
    /// This is good for creating generic attention systems that are shown for
    /// the same way for any voice based service active in the scene.
    /// </summary>
    //[Tooltip("Finds all voice based services and listens for changes in their audio input state.")]
    public class CombinedAudioEventReference : AudioInputServiceReference, IAudioInputEvents
    {
        public override IAudioInputEvents AudioEvents => this;

        private WitMicLevelChangedEvent _onMicAudioLevelChanged = new WitMicLevelChangedEvent();
        private UnityEvent _onMicStartedListening = new UnityEvent();
        private UnityEvent _onMicStoppedListening = new UnityEvent();
        private AudioEventListener[] _sourceListeners;

        private void Awake()
        {
            #if UNITY_2020_1_OR_NEWER
            _sourceListeners = FindObjectsOfType<AudioEventListener>(true);
            #else
            _sourceListeners = FindObjectsOfType<AudioEventListener>();
            #endif
        }

        private void OnEnable()
        {
            foreach (var listener in _sourceListeners)
            {
                listener.OnMicAudioLevelChanged.AddListener(OnMicAudioLevelChanged.Invoke);
                listener.OnMicStartedListening.AddListener(OnMicStartedListening.Invoke);
                listener.OnMicStoppedListening.AddListener(OnMicStoppedListening.Invoke);
            }
        }

        private void OnDisable()
        {
            foreach (var listener in _sourceListeners)
            {
                listener.OnMicAudioLevelChanged.RemoveListener(OnMicAudioLevelChanged.Invoke);
                listener.OnMicStartedListening.RemoveListener(OnMicStartedListening.Invoke);
                listener.OnMicStoppedListening.RemoveListener(OnMicStoppedListening.Invoke);
            }
        }

        public WitMicLevelChangedEvent OnMicAudioLevelChanged => _onMicAudioLevelChanged;
        public UnityEvent OnMicStartedListening => _onMicStartedListening;
        public UnityEvent OnMicStoppedListening => _onMicStoppedListening;
    }
}
