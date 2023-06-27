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
    /// The states of a voice request
    /// </summary>
    public enum VoiceRequestState
    {
        /// <summary>
        /// Request has been generated but not transmitting.  Request parameters can still be adjusted.
        /// </summary>
        Initialized,

        /// <summary>
        /// Request has begun transmission.
        /// </summary>
        Transmitting,

        /// <summary>
        /// Request has been canceled.
        /// </summary>
        Canceled,

        /// <summary>
        /// Request has failed to complete.
        /// </summary>
        Failed,

        /// <summary>
        /// Request completed successfully.
        /// </summary>
        Successful
    }
}
