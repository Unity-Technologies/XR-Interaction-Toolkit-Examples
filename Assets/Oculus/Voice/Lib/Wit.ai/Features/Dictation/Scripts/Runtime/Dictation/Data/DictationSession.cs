/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data;

namespace Meta.WitAi.Dictation.Data
{
    [Serializable]
    public class DictationSession : VoiceSession
    {
        /// <summary>
        /// Dictation service being used
        /// </summary>
        public IDictationService dictationService;
        /// <summary>
        /// Collection of Request IDs generated from client for Wit requests
        /// </summary>
        public string[] clientRequestId;
        /// <summary>
        /// An identifier for the current dictation session
        /// </summary>
        public string sessionId = Guid.NewGuid().ToString();
    }
}
