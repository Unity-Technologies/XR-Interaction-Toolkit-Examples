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
    [AddComponentMenu("Wit.ai/Response Matchers/Simple String Entity Handler")]
    public class SimpleStringEntityHandler : WitIntentMatcher
    {
        [SerializeField] public string entity;
        [SerializeField] public string format;
        [SerializeField] private StringEntityMatchEvent onIntentEntityTriggered
            = new StringEntityMatchEvent();

        public StringEntityMatchEvent OnIntentEntityTriggered => onIntentEntityTriggered;

        protected override string OnValidateResponse(WitResponseNode response, bool isEarlyResponse)
        {
            // Return base
            string result = base.OnValidateResponse(response, isEarlyResponse);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            // Not found
            var entityValue = response.GetFirstEntityValue(entity);
            if (string.IsNullOrEmpty(entityValue))
            {
                return $"Missing required entity: {entity}";
            }
            // Fail
            return string.Empty;
        }
        protected override void OnResponseInvalid(WitResponseNode response, string error) { }
        protected override void OnResponseSuccess(WitResponseNode response)
        {
            var entityValue = response.GetFirstEntityValue(entity);
            if (!string.IsNullOrEmpty(format))
            {
                onIntentEntityTriggered.Invoke(format.Replace("{value}", entityValue));
            }
            else
            {
                onIntentEntityTriggered.Invoke(entityValue);
            }
        }
    }

    [Serializable]
    public class StringEntityMatchEvent : UnityEvent<string> {}
}
