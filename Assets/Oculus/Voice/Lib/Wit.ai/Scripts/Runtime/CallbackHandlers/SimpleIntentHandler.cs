/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.CallbackHandlers
{
    [AddComponentMenu("Wit.ai/Response Matchers/Simple Intent Handler")]
    public class SimpleIntentHandler : WitIntentMatcher
    {
        [SerializeField] private UnityEvent onIntentTriggered = new UnityEvent();

        [Tooltip("Confidence ranges are executed in order. If checked, all confidence values will be checked instead of stopping on the first one that matches.")]
        [SerializeField] public bool allowConfidenceOverlap;
#if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
#endif
        [SerializeField] public ConfidenceRange[] confidenceRanges;

        public UnityEvent OnIntentTriggered => onIntentTriggered;

        protected override void OnResponseSuccess(WitResponseNode response)
        {
            onIntentTriggered.Invoke();
            UpdateRanges(response);
        }
        protected override void OnResponseInvalid(WitResponseNode response, string error)
        {
            UpdateRanges(response);
        }

        private void UpdateRanges(WitResponseNode response)
        {
            // Find intents if possible
            var intents = response?.GetIntents();
            if (intents == null)
            {
                return;
            }

            // Iterate intents
            foreach (var intentData in intents)
            {
                if (string.Equals(intent, intentData.name, StringComparison.CurrentCultureIgnoreCase))
                {
                    // Found intent
                    RefreshConfidenceRange(intentData.confidence, confidenceRanges, allowConfidenceOverlap);
                    return;
                }
            }

            // Not matched
            RefreshConfidenceRange(0, confidenceRanges, allowConfidenceOverlap);
        }
    }
}
