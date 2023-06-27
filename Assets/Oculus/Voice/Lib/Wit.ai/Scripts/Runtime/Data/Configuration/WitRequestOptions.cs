/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Meta.WitAi.Interfaces;
using UnityEngine;

namespace Meta.WitAi.Configuration
{
    public class WitRequestOptions : VoiceServiceRequestOptions
    {
        /// <summary>
        /// An interface that provides a list of entities that should be used for nlu resolution.
        /// </summary>
        public IDynamicEntitiesProvider dynamicEntities;

        /// <summary>
        /// The maximum number of intent matches to return
        /// </summary>
        public int nBestIntents = -1;

        /// <summary>
        /// The tag for snapshot
        /// </summary>
        public string tag;

        /// <summary>
        /// Formerly used for request id
        /// </summary>
        [Obsolete("Use 'RequestId' property instead")] [JsonIgnore]
        public string requestID => RequestId;

        /// <summary>
        /// Additional parameters to be used for custom
        /// implementation overrides.
        /// </summary>
        public Dictionary<string, string> additionalParameters = new Dictionary<string, string>();

        /// <summary>
        /// Callback for completion
        /// </summary>
        public Action<WitRequest> onResponse;

        // Get json string
        public string ToJsonString()
        {
            // Get default json
            string results = JsonUtility.ToJson(this);

            // Append parameters before final }
            StringBuilder parameters = new StringBuilder();
            foreach (var key in additionalParameters.Keys)
            {
                string value = additionalParameters[key].Replace("\"", "\\\"");
                parameters.Append($",\"{key}\":\"{value}\"");
            }
            results = results.Insert(results.Length - 1, parameters.ToString());

            // Return json
            return results;
        }
    }
}
