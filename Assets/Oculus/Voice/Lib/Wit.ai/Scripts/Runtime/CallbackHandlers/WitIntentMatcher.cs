/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Conduit;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Intents;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.WitAi.CallbackHandlers
{
    // Abstract class to share confidence handling
    public abstract class WitIntentMatcher : WitResponseHandler
    {
        /// <summary>
        /// Intent name to be matched
        /// </summary>
        [Header("Intent Settings")]
        [SerializeField] public string intent;

        /// <summary>
        /// Confidence threshold
        /// </summary>
        [FormerlySerializedAs("confidence")]
        [Range(0, 1f), SerializeField] public float confidenceThreshold = .6f;

        // Handle simple intent validation
        protected override string OnValidateResponse(WitResponseNode response, bool isEarlyResponse)
        {
            // No response
            if (response == null)
            {
                return "No response";
            }
            // Check against all intents
            WitIntentData[] intents = response.GetIntents();
            if (intents == null || intents.Length == 0)
            {
                return "No intents found";
            }
            // Find intent
            WitIntentData found = null;
            foreach (var intentData in intents)
            {
                if (string.Equals(intent, intentData.name, StringComparison.CurrentCultureIgnoreCase))
                {
                    found = intentData;
                    break;
                }
            }
            if (found == null)
            {
                return $"Missing required intent '{intent}'";
            }
            // Check confidence
            if (found.confidence < confidenceThreshold)
            {
                return $"Required intent '{intent}' confidence too low: {found.confidence:0.000}\nRequired: {confidenceThreshold:0.000}";
            }
            return string.Empty;
        }

        protected override void OnEnable()
        {
            Manifest.WitResponseMatcherIntents.Add(intent);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Manifest.WitResponseMatcherIntents.Remove(intent);
            base.OnDisable();
        }
    }
}
