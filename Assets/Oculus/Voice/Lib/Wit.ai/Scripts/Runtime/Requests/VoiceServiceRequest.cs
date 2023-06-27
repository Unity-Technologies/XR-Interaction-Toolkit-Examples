/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Json;

namespace Meta.WitAi.Requests
{
    [Serializable]
    public abstract class VoiceServiceRequest
        : NLPRequest<VoiceServiceRequestEvent, WitRequestOptions, VoiceServiceRequestEvents, VoiceServiceRequestResults>
    {
        /// <summary>
        /// Constructor for Voice Service requests
        /// </summary>
        /// <param name="newInputType">The request input type (text/audio) to be used</param>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected VoiceServiceRequest(NLPRequestInputType newInputType, WitRequestOptions newOptions, VoiceServiceRequestEvents newEvents) : base(newInputType, newOptions, newEvents) {}

        /// <summary>
        /// The status code returned from the last request
        /// </summary>
        public int StatusCode
        {
            get => Results == null ? 0 : Results.StatusCode;
            protected set
            {
                int newCode = value;
                if (newCode.Equals(Results == null ? 0 : Results.StatusCode))
                {
                    return;
                }
                if (Results == null)
                {
                    Results = new VoiceServiceRequestResults();
                }
                Results.StatusCode = newCode;
            }
        }

        /// <summary>
        /// Returns an empty result object with the current status code
        /// </summary>
        /// <param name="newMessage">The message to be set on the results</param>
        protected override VoiceServiceRequestResults GetResultsWithMessage(string newMessage)
        {
            VoiceServiceRequestResults results = new VoiceServiceRequestResults(newMessage);
            results.StatusCode = StatusCode;
            return results;
        }

        /// <summary>
        /// Applies a transcription to the current results
        /// </summary>
        /// <param name="newTranscription">The transcription returned</param>
        /// <param name="newIsFinal">Whether the transcription has completed building</param>
        protected override void ApplyTranscription(string newTranscription, bool newIsFinal)
        {
            if (Results == null)
            {
                Results = new VoiceServiceRequestResults();
            }
            Results.Transcription = newTranscription;
            Results.IsFinalTranscription = newIsFinal;
            if (Results.IsFinalTranscription)
            {
                List<string> transcriptions = new List<string>();
                if (Results.FinalTranscriptions != null)
                {
                    transcriptions.AddRange(Results.FinalTranscriptions);
                }
                transcriptions.Add(Results.Transcription);
                Results.FinalTranscriptions = transcriptions.ToArray();
            }
            OnTranscriptionChanged();
        }

        /// <summary>
        /// Applies response data to the current results
        /// </summary>
        /// <param name="newData">The returned response data</param>
        protected override void ApplyResultResponseData(WitResponseNode newData)
        {
            if (Results == null)
            {
                Results = new VoiceServiceRequestResults();
            }
            Results.ResponseData = newData;
        }

        /// <summary>
        /// Performs an event callback with this request as the parameter
        /// </summary>
        /// <param name="eventCallback">The voice service request event to be called</param>
        protected override void RaiseEvent(VoiceServiceRequestEvent eventCallback)
        {
            eventCallback?.Invoke(this);
        }
    }
}
