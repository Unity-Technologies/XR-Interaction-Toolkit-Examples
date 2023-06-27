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
    /// Interface for text language processing request parameters
    /// </summary>
    public interface INLPTextRequestOptions : INLPRequestOptions
    {
        /// <summary>
        /// The text to be processed via the NLP request
        /// </summary>
        string Text { get; set; }
    }
}
