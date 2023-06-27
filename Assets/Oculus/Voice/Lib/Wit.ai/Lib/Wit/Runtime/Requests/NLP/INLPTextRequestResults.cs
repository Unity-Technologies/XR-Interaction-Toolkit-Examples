/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Voice
{
    /// <summary>
    /// Interface for text language processing request results
    /// </summary>
    /// <typeparam name="TResultData">Type of NLP data received from the request</typeparam>
    public interface INLPTextRequestResults
        : INLPRequestResults
    {
    }
}
