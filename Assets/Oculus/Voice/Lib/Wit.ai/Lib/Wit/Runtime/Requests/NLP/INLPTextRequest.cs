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
    /// Interface for text based language processing requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public interface INLPTextRequest<TUnityEvent, TOptions, TEvents, TResults>
        : IVoiceRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : INLPTextRequestOptions
        where TEvents : INLPTextRequestEvents<TUnityEvent>
        where TResults : INLPTextRequestResults
    {
        /// <summary>
        /// Send text via a NLP request
        /// </summary>
        /// <param name="text">The text to be processed via NLP</param>
        void Send(string text);
    }
}
