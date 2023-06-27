/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

namespace Meta.Conduit
{
    using System;

    /// <summary>
    /// Marks the method as a callback for voice commands. The method will be mapped to an intent and invoked whenever
    /// that intent is resolved by the backend.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ConduitActionAttribute : Attribute
    {
        /// <summary>
        /// The intent name matching this method. If left blank, the method name will be used to infer the intent name.
        /// </summary>
        public string Intent { get; private set; }

        /// <summary>
        /// The minimum confidence value for intent matching
        /// </summary>
        public float MinConfidence { get; protected set; }
        // Default minimum confidence
        protected const float DEFAULT_MIN_CONFIDENCE = 0.51f;

        /// <summary>
        /// The maximum confidence value for intent matching
        /// </summary>
        public float MaxConfidence { get; protected set; }
        // Default maximum confidence
        protected const float DEFAULT_MAX_CONFIDENCE = 1.0f;

        /// <summary>
        /// Additional aliases to refer to the intent this method represent.
        /// </summary>
        public List<string> Aliases { get; private set; }

        /// <summary>
        /// If true, this will be called for partial responses
        /// instead of full responses.  It will also contain a VoiceSession
        /// parameter which can be used to 'validate' a partial response so
        /// the VoiceSession treats the response as final & deactivates.
        /// </summary>
        public bool ValidatePartial { get; private set; }

        /// <summary>
        /// Triggers a method to be executed if it matches a voice command's intent.
        /// </summary>
        /// <param name="intent">The name of the intent to match.</param>
        protected ConduitActionAttribute(string intent = "", params string[] aliases)
        {
            this.Intent = intent;
            this.Aliases = aliases.ToList();
        }

        /// <summary>
        /// Triggers a method to be executed if it matches a voice command's intent.
        /// </summary>
        /// <param name="intent">The name of the intent to match.</param>
        /// <param name="minConfidence">The minimum confidence value (0-1) needed to match.</param>
        /// <param name="maxConfidence">The maximum confidence value(0-1) needed to match.</param>
        /// <param name="validatePartial">When true will validate partial matches.</param>
        /// <param name="aliases">Other names to refer to this intent.</param>
        protected ConduitActionAttribute(string intent = "", float minConfidence = DEFAULT_MIN_CONFIDENCE, float maxConfidence = DEFAULT_MAX_CONFIDENCE, bool validatePartial = false, params string[] aliases)
        {
            this.Intent = intent;
            this.MinConfidence = minConfidence;
            this.MaxConfidence = maxConfidence;
            this.ValidatePartial = validatePartial;
            this.Aliases = aliases.ToList();
        }
    }
}
