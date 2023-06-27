/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Conduit;

namespace Meta.WitAi
{
    /// <summary>
    /// Triggers a method to be executed if it matches a voice command's intent
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MatchIntent : ConduitActionAttribute
    {
        /// <summary>
        /// Triggers a method to be executed if it matches a voice command's intent
        /// </summary>
        /// <param name="intent">The name of the intent to match</param>
        /// <param name="minConfidence">The minimum confidence value (0-1) needed to match</param>
        /// <param name="maxConfidence">The maximum confidence value(0-1) needed to match</param>
        public MatchIntent(string intent, float minConfidence = DEFAULT_MIN_CONFIDENCE, float maxConfidence = DEFAULT_MAX_CONFIDENCE) : base(intent, minConfidence, maxConfidence, false)
        {
        }
    }
}
