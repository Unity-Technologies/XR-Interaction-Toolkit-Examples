/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Events;

namespace Meta.WitAi.Interfaces
{
    public interface ITranscriptionEvent
    {
        /// <summary>
        /// Message fired when a partial transcription has been received.
        /// </summary>
        WitTranscriptionEvent OnPartialTranscription { get; }

        /// <summary>
        /// Message received when a complete transcription is received.
        /// </summary>
        WitTranscriptionEvent OnFullTranscription { get; }
    }
}
