/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Json;

namespace Meta.Voice
{
    /// <summary>
    /// Interface for NLP request results
    /// </summary>
    /// <typeparam name="TResultData">Type of NLP data received from the request</typeparam>
    public interface INLPRequestResults : IVoiceRequestResults
    {
        /// <summary>
        /// Processed data from the request
        /// Should only be set by NLPRequests
        /// </summary>
        WitResponseNode ResponseData { get; }
    }
}
