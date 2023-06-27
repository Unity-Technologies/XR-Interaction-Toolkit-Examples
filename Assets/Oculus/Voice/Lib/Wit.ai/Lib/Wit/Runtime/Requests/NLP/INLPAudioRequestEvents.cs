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
    /// Interface for all specific audio NLP request events
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of unity event to be called for all events</typeparam>
    public interface INLPAudioRequestEvents<TUnityEvent> : INLPRequestEvents<TUnityEvent>, ITranscriptionRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {
        /// <summary>
        /// Called on request language processing while audio is still being analyzed
        /// </summary>
        NLPRequestResponseEvent OnPartialResponse { get; }
    }
}
