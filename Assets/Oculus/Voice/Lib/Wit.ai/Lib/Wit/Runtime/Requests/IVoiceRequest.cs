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
    /// All publicly accessible properties & methods of a voice request
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public interface IVoiceRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : IVoiceRequestOptions
        where TEvents : IVoiceRequestEvents<TUnityEvent>
        where TResults : IVoiceRequestResults
    {
        /// <summary>
        /// The states of a voice request
        /// </summary>
        VoiceRequestState State { get; }
        /// <summary>
        /// Download progress of the current request transmission
        /// if available
        /// </summary>
        float DownloadProgress { get; }
        /// <summary>
        /// Upload progress of the current request transmission
        /// if available
        /// </summary>
        float UploadProgress { get; }

        /// <summary>
        /// Options sent as the request parameters
        /// </summary>
        TOptions Options { get; }

        /// <summary>
        /// Events specific to this voice request
        /// </summary>
        TEvents Events { get; }

        /// <summary>
        /// Results returned from the request
        /// </summary>
        TResults Results { get; }

        /// <summary>
        /// Whether a transmission is permitted
        /// </summary>
        bool CanSend { get; }

        /// <summary>
        /// Begin the transmission of data
        /// </summary>
        void Send();

        /// <summary>
        /// Cancel the request immediately with a
        /// specified message
        /// </summary>
        void Cancel(string message);
    }
}
