/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.WitAi.Requests
{
    /// <summary>
    /// A class used to track Wit text 'Message' requests
    /// </summary>
    [Serializable]
    public class WitUnityRequest : VoiceServiceRequest
    {
        /// <summary>
        /// The configuration to be used for the request
        /// </summary>
        public WitConfiguration Configuration { get; private set; }
        /// <summary>
        /// Unity request wrapper for Wit
        /// </summary>
        private readonly WitVRequest _request;
        /// <summary>
        /// Endpoint to be used
        /// </summary>
        public string Endpoint { get; set; }
        /// <summary>
        /// Endpoint to be used
        /// </summary>
        public bool ShouldPost { get; set; }

        /// <summary>
        /// Apply configuration
        /// </summary>
        /// <param name="newConfiguration"></param>
        /// <param name="newOptions"></param>
        /// <param name="newEvents"></param>
        public WitUnityRequest(WitConfiguration newConfiguration, NLPRequestInputType newDataType, WitRequestOptions newOptions, VoiceServiceRequestEvents newEvents) : base(newDataType, newOptions, newEvents)
        {
            // Apply configuration & generate request
            Configuration = newConfiguration;

            // Generate a message WitVRequest
            if (InputType == NLPRequestInputType.Text)
            {
                _request = new WitMessageVRequest(Configuration, newOptions.RequestId);
                Endpoint = Configuration.GetEndpointInfo().Message;
                _request.Timeout = Mathf.RoundToInt(Configuration.timeoutMS / 1000f);
                ShouldPost = false;
            }
            // Generate an audio WitVRequest
            else if (InputType == NLPRequestInputType.Audio)
            {
                // TODO: T121060485: Add audio support to WitVRequest
                Endpoint = Configuration.GetEndpointInfo().Speech;
                ShouldPost = true;
            }

            // Initialized
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
        /// Get send error options
        /// </summary>
        protected override string GetSendError()
        {
            if (Configuration == null)
            {
                return "Cannot send request without a valid configuration.";
            }
            if (_request == null)
            {
                return "Request creation failed.";
            }
            return base.GetSendError();
        }

        /// <summary>
        /// Performs a wit message request
        /// </summary>
        /// <param name="onSendComplete">Callback that handles send completion</param>
        protected override void HandleSend()
        {
            // Send message request
            if (_request is WitMessageVRequest messageRequest)
            {
                messageRequest.MessageRequest(Endpoint, ShouldPost,
                    Options.Text, Options.QueryParams,
                    HandleNlpResponse, SetDownloadProgress);
            }
        }

        /// <summary>
        /// Set status code prior to handling response
        /// </summary>
        protected override void HandleNlpResponse(WitResponseNode responseData, string error)
        {
            StatusCode = _request.ResponseCode;
            base.HandleNlpResponse(responseData, error);
        }

        /// <summary>
        /// Handle cancellation
        /// </summary>
        protected override void HandleCancel()
        {
            if (_request != null)
            {
                _request.Cancel();
            }
        }

        #region AUDIO CALLBACKS
        // TODO: T121060485: Add audio support to WitVRequest
        /// <summary>
        /// Error returned if audio cannot be activated
        /// </summary>
        protected override string GetActivateAudioError()
        {
            return "Audio request not yet implemented";
        }
        /// <summary>
        /// Activates audio & calls activated callback once complete
        /// </summary>
        protected override void HandleAudioActivation()
        {
            SetAudioInputState(VoiceAudioInputState.On);
        }
        /// <summary>
        /// Deactivates audio asap & calls deactivated callback once complete
        /// </summary>
        protected override void HandleAudioDeactivation()
        {
            SetAudioInputState(VoiceAudioInputState.Off);
        }
        #endregion
    }
}
