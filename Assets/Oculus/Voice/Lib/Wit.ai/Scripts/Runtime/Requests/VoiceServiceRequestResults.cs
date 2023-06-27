/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice;
using Meta.WitAi.Json;

namespace Meta.WitAi.Requests
{
    public class VoiceServiceRequestResults
        : INLPTextRequestResults, INLPAudioRequestResults
    {
        /// <summary>
        /// Request status code if applicable
        /// </summary>
        public int StatusCode { get; internal set; }
        /// <summary>
        /// Request cancelation/error message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Response transcription
        /// </summary>
        public string Transcription { get; internal set; }
        /// <summary>
        /// Response transcription
        /// </summary>
        public bool IsFinalTranscription { get; internal set; }
        /// <summary>
        /// Response transcription
        /// </summary>
        public string[] FinalTranscriptions { get; internal set; }
        /// <summary>
        /// Parsed json response data
        /// </summary>
        public WitResponseNode ResponseData { get; internal set; }

        /// <summary>
        /// Default constructor without message
        /// </summary>
        public VoiceServiceRequestResults()
        {
            Message = string.Empty;
        }
        /// <summary>
        /// Constructor with a specific message
        /// </summary>
        public VoiceServiceRequestResults(string newMessage)
        {
            Message = newMessage;
        }
    }
}
