/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// The various types of NLP request datas
    /// </summary>
    public enum NLPRequestInputType
    {
        Text,
        Audio
    }

    /// <summary>
    /// Abstract class for NLP text & audio requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public abstract class NLPRequest<TUnityEvent, TOptions, TEvents, TResults>
        : NLPAudioRequest<TUnityEvent, TOptions, TEvents, TResults>,
            INLPTextRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : INLPAudioRequestOptions,
            INLPTextRequestOptions
        where TEvents : NLPAudioRequestEvents<TUnityEvent>,
            INLPTextRequestEvents<TUnityEvent>
        where TResults : INLPAudioRequestResults,
            INLPTextRequestResults
    {
        /// <summary>
        /// The request data input type to be used
        /// </summary>
        public NLPRequestInputType InputType { get; private set; }

        /// <summary>
        /// Constructor for NLP text & audio requests
        /// </summary>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected NLPRequest(NLPRequestInputType newInputType, TOptions newOptions, TEvents newEvents) : base(newOptions,
            newEvents)
        {
            InputType = newInputType;
            _initialized = true;
            SetState(VoiceRequestState.Initialized);
        }
        /// <summary>
        /// Ignore state changes unless setup
        /// </summary>
        private bool _initialized = false;
        protected override void SetState(VoiceRequestState newState)
        {
            if (_initialized)
            {
                base.SetState(newState);
            }
        }
        /// <summary>
        /// Append NLP request specific data to log
        /// </summary>
        /// <param name="log">Building log</param>
        /// <param name="warning">True if this is a warning log</param>
        protected override void AppendLogData(StringBuilder log, bool warning)
        {
            base.AppendLogData(log, warning);
            // Append nlp input
            log.AppendLine($"NLP Input Type: {InputType}");
        }

        /// <summary>
        /// Throw error on text request
        /// </summary>
        protected override string GetActivateAudioError()
        {
            if (InputType == NLPRequestInputType.Text)
            {
                return "Cannot activate audio on a text request";
            }
            return string.Empty;
        }

        /// <summary>
        /// Throw error on text request
        /// </summary>
        protected override string GetSendError()
        {
            if (InputType == NLPRequestInputType.Audio && !IsAudioInputActivated)
            {
                return "Cannot send audio without activation";
            }
            return base.GetSendError();
        }

        /// <summary>
        /// Applies the specified text to the options if possible & then submits
        /// </summary>
        public void Send(string text)
        {
            // Ignore unless text event
            if (InputType != NLPRequestInputType.Text)
            {
                LogW($"Request Text Ignored\nReason: Request only accepts audio input");
                return;
            }
            // Apply text if will be able to send
            if (CanSend)
            {
                Options.Text = text;
            }
            // Send
            Send();
        }
    }
}
