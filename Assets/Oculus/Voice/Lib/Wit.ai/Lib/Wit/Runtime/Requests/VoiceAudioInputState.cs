/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.Voice
{
    /// <summary>
    /// The various audio input states
    /// </summary>
    public enum VoiceAudioInputState
    {
        /// <summary>
        /// Not listening to audio
        /// </summary>
        Off,

        /// <summary>
        /// Enabling audio input
        /// </summary>
        Activating,

        /// <summary>
        /// Listening to audio
        /// </summary>
        On,

        /// <summary>
        /// Disabling audio input
        /// </summary>
        Deactivating
    }
}
