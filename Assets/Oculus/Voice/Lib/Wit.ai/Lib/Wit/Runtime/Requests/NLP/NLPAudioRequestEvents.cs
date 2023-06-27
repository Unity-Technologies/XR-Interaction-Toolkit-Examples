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

namespace Meta.Voice
{
    [Serializable]
    public class NLPAudioRequestEvents<TUnityEvent>
        : TranscriptionRequestEvents<TUnityEvent>,
            INLPAudioRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {
        /// <summary>
        /// Called on request language processing while audio is still being analyzed
        /// </summary>
        public NLPRequestResponseEvent OnPartialResponse => _onPartialResponse;
        [Header("NLP Events")] [Tooltip("Called every time audio input changes states.")]
        [SerializeField] private NLPRequestResponseEvent _onPartialResponse = Activator.CreateInstance<NLPRequestResponseEvent>();

        /// <summary>
        /// Called on request language processing once completely analyzed
        /// </summary>
        public NLPRequestResponseEvent OnFullResponse => _onFullResponse;
        [Tooltip("Called on request language processing once completely analyzed.")]
        [SerializeField] private NLPRequestResponseEvent _onFullResponse = Activator.CreateInstance<NLPRequestResponseEvent>();
    }
}
