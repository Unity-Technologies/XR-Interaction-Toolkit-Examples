/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi.TTS.Data
{
    public abstract class TTSVoiceSettings
    {
        // Used for initial value
        public const string DEFAULT_ID = "Default Voice";

        /// <summary>
        /// The unique voice settings id
        /// </summary>
        public string settingsID = DEFAULT_ID;
    }
}
