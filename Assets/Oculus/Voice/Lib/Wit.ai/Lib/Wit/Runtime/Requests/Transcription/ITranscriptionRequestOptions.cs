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
    /// Interface for audio transcription specific options
    /// </summary>
    public interface ITranscriptionRequestOptions : IVoiceRequestOptions
    {
        /// <summary>
        /// The audio threshold that must be surpassed to begin an activation.
        /// If less than or equal to 0, then always Send immediately
        /// </summary>
        float AudioThreshold { get; }
    }
}
