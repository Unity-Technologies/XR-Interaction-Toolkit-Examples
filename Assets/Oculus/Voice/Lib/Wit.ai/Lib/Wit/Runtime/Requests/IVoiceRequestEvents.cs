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
    /// An interface base for all voice request events
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of unity event to be called for all events</typeparam>
    public interface IVoiceRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {
        /// <summary>
        /// Called whenever a request state changes
        /// </summary>
        TUnityEvent OnStateChange { get; }
        /// <summary>
        /// Called on download progress update
        /// </summary>
        TUnityEvent OnDownloadProgressChange { get; }
        /// <summary>
        /// Called on upload progress update
        /// </summary>
        TUnityEvent OnUploadProgressChange { get; }

        /// <summary>
        /// Called on initial request generation
        /// </summary>
        TUnityEvent OnInit { get; }
        /// <summary>
        /// Called following the start of data transmission
        /// </summary>
        TUnityEvent OnSend { get; }
        /// <summary>
        /// Called following the cancellation of a request
        /// </summary>
        TUnityEvent OnCancel { get; }
        /// <summary>
        /// Called following an error response from a request
        /// </summary>
        TUnityEvent OnFailed { get; }
        /// <summary>
        /// Called following a successful request & data parse with results provided
        /// </summary>
        TUnityEvent OnSuccess { get; }
        /// <summary>
        /// Called following cancellation, failure or success to finalize request.
        /// </summary>
        TUnityEvent OnComplete { get; }
    }
}
