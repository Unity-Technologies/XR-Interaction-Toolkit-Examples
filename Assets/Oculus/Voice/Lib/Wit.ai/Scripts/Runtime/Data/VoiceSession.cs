/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;

namespace Meta.WitAi.Data
{
    [Serializable]
    public class VoiceSession
    {
        /// <summary>
        /// Voice service being used
        /// </summary>
        public VoiceService service;
        /// <summary>
        /// Voice service response data
        /// </summary>
        public WitResponseNode response;
        /// <summary>
        /// Session response data is valid & can be deactivated if true
        /// </summary>
        public bool validResponse = false;
    }
}
