/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Meta.WitAi.Json;

namespace Meta.WitAi.Requests
{
    internal class WitMessageVRequest : WitVRequest
    {
        // Constructor
        public WitMessageVRequest(IWitRequestConfiguration configuration, string requestId) : base(configuration, requestId, false) {}

        /// <summary>
        /// Voice message request
        /// </summary>
        /// <param name="text">Text to be sent to message endpoint</param>
        /// <param name="queryParams">Parameters to be sent to the endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool MessageRequest(string text,
            Dictionary<string, string> queryParams,
            RequestCompleteDelegate<WitResponseNode> onComplete,
            RequestProgressDelegate onProgress = null) =>
            MessageRequest(WitConstants.ENDPOINT_MESSAGE, false, text, queryParams, onComplete, onProgress);
        /// <summary>
        /// Voice message request
        /// </summary>
        /// <param name="endpoint">Endpoint to be used for possible overrides</param>
        /// <param name="post">Will perform a POST if true, will perform a GET otherwise</param>
        /// <param name="text">Text to be sent to message endpoint</param>
        /// <param name="queryParams">Parameters to be sent to the endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool MessageRequest(string endpoint, bool post, string text,
            Dictionary<string, string> queryParams,
            RequestCompleteDelegate<WitResponseNode> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Add text to uri parameters
            Dictionary<string, string> uriParams = queryParams ?? new Dictionary<string, string>();

            // Perform a get request
            if (!post)
            {
                uriParams[WitConstants.ENDPOINT_MESSAGE_PARAM] = text;
                return RequestWitGet(endpoint, uriParams, onComplete, onProgress);
            }
            // Perform a post request
            else
            {
                return RequestWitPost(endpoint, uriParams, text, onComplete, onProgress);
            }
        }
    }
}
