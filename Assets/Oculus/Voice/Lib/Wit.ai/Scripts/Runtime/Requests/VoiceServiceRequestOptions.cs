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

namespace Meta.WitAi.Requests
{
    public class VoiceServiceRequestOptions : INLPAudioRequestOptions, INLPTextRequestOptions
    {
        /// <summary>
        /// Unique request id used for request tracking internally & externally
        /// </summary>
        public string RequestId { get; private set; }
        /// <summary>
        /// Additional request query parameters to be sent with the request
        /// </summary>
        public Dictionary<string, string> QueryParams { get; private set; }
        public class QueryParam
        {
            public string key;
            public string value;
        }

        /// <summary>
        /// The text to be submitted for a text request
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The threshold to be used for an audio request
        /// </summary>
        public float AudioThreshold { get; set; }

        /// <summary>
        /// Setup with a randomly generated guid
        /// </summary>
        public VoiceServiceRequestOptions(params QueryParam[] newParams)
        {
            RequestId = GetUniqueRequestId();
            QueryParams = ConvertQueryParams(newParams);
        }
        /// <summary>
        /// Setup with a specific guid
        /// </summary>
        public VoiceServiceRequestOptions(string newRequestId, params QueryParam[] newParams)
        {
            RequestId = string.IsNullOrEmpty(newRequestId) ? GetUniqueRequestId() : newRequestId;
            QueryParams = ConvertQueryParams(newParams);
        }
        /// <summary>
        /// Generates a random guid
        /// </summary>
        protected virtual string GetUniqueRequestId() => Guid.NewGuid().ToString();
        /// <summary>
        /// Generates a dictionary of key/value strings from a query param array
        /// </summary>
        public static Dictionary<string, string> ConvertQueryParams(QueryParam[] newParams)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var param in newParams)
            {
                if (!string.IsNullOrEmpty(param.key))
                {
                    results[param.key] = results[param.value];
                }
            }
            return results;
        }
    }
}
