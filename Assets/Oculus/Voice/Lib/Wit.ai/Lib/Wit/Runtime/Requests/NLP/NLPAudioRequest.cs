/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;
using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// Abstract class for all NLP audio requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public abstract class NLPAudioRequest<TUnityEvent, TOptions, TEvents, TResults>
        : TranscriptionRequest<TUnityEvent, TOptions, TEvents, TResults>,
            INLPAudioRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : INLPAudioRequestOptions
        where TEvents : NLPAudioRequestEvents<TUnityEvent>
        where TResults : INLPAudioRequestResults
    {
        /// <summary>
        /// Constructor class for NLP audio requests
        /// </summary>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected NLPAudioRequest(TOptions newOptions, TEvents newEvents) : base(newOptions, newEvents) {}

        /// <summary>
        /// Set response data early if possible
        /// </summary>
        public WitResponseNode ResponseData
        {
            get => Results?.ResponseData;
            protected set
            {
                // Ignore if same
                WitResponseNode newData = value;
                if (newData == null || newData.Equals(Results?.ResponseData))
                {
                    return;
                }

                // Apply response data
                ApplyResultResponseData(newData);
                OnResponseDataChanged();
            }
        }

        /// <summary>
        /// Applies response data to the current results
        /// </summary>
        /// <param name="newData">The returned response data</param>
        protected abstract void ApplyResultResponseData(WitResponseNode newData);

        /// <summary>
        /// Called when response data has been updated
        /// </summary>
        protected virtual void OnResponseDataChanged()
        {
            Events?.OnPartialResponse?.Invoke(ResponseData);
        }

        /// <summary>
        /// Method to be called when an NLP request had completed
        /// </summary>
        /// <param name="responseData">Parsed json data returned from request</param>
        /// <param name="error">Error returned from a request</param>
        protected virtual void HandleNlpResponse(WitResponseNode responseData, string error)
        {
            // Ignore if not in correct state
            if (State != VoiceRequestState.Initialized && State != VoiceRequestState.Transmitting)
            {
                return;
            }

            // Error returned
            if (!string.IsNullOrEmpty(error))
            {
                HandleFailure(error);
            }
            // No response
            else if (responseData == null)
            {
                HandleFailure("No response returned");
            }
            // Success
            else
            {
                ResponseData = responseData;
                Events?.OnFullResponse?.Invoke(ResponseData);
                HandleSuccess(Results);
            }
        }

        /// <summary>
        /// Cancels the current request but handles success immediately if possible
        /// </summary>
        public virtual void CompleteEarly()
        {
            // Ignore if not in correct state
            if (State != VoiceRequestState.Initialized && State != VoiceRequestState.Transmitting)
            {
                return;
            }

            // Cancel instead
            if (ResponseData == null)
            {
                Cancel("Cannot complete early without response data");
            }
            // Handle success
            else
            {
                HandleNlpResponse(ResponseData, string.Empty);
            }
        }
    }
}
