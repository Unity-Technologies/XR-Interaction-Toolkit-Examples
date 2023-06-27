/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;

namespace Meta.WitAi.TTS.Interfaces
{
    public interface ISpeakerTextPreprocessor
    {

        /// <summary>
        /// Called before prefix/postfix modifications are applied to the input string
        /// </summary>
        /// <param name="speaker">The speaker that will be used to speak the resulting text</param>
        /// <param name="phrases">The current phrase list that will be used for speech.  Can be added to or removed as needed.</param>
        void OnPreprocessTTS(TTSSpeaker speaker, List<string> phrases);
    }

    public interface ISpeakerTextPostprocessor
    {
        /// <summary>
        /// Called after prefix/postfix modifications are applied to the input string
        /// </summary>
        /// <param name="speaker">The speaker that will be used to speak the resulting text</param>
        /// <param name="phrases">The current phrase list that will be used for speech.  Can be added to or removed as needed.</param>
        void OnPostprocessTTS(TTSSpeaker speaker, List<string> phrases);
    }
}
