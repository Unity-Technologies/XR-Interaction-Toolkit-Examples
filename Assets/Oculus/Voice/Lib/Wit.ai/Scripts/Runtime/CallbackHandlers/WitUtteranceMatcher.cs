/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text.RegularExpressions;
using Meta.WitAi.Json;
using Meta.WitAi.Utilities;
using UnityEngine;

namespace Meta.WitAi.CallbackHandlers
{
    [AddComponentMenu("Wit.ai/Response Matchers/Utterance Matcher")]
    public class WitUtteranceMatcher : WitResponseHandler
    {
        [SerializeField] private string searchText;
        [SerializeField] private bool exactMatch = true;
        [SerializeField] private bool useRegex;

        [SerializeField] private StringEvent onUtteranceMatched = new StringEvent();

        private Regex regex;

        protected override string OnValidateResponse(WitResponseNode response, bool isEarlyResponse)
        {
            var text = response["text"].Value;
            if (!IsMatch(text))
            {
                return "Required utterance does not match";
            }
            return "";
        }
        protected override void OnResponseInvalid(WitResponseNode response, string error){}
        protected override void OnResponseSuccess(WitResponseNode response)
        {
            var text = response["text"].Value;
            onUtteranceMatched?.Invoke(text);
        }

        private bool IsMatch(string text)
        {
            if (useRegex)
            {
                if (null == regex)
                {
                    regex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                var match = regex.Match(text);
                if (match.Success)
                {
                    if (exactMatch && match.Value == text)
                    {
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else if (exactMatch && text.ToLower() == searchText.ToLower())
            {
                return true;
            }
            else if (text.ToLower().Contains(searchText.ToLower()))
            {
                return true;
            }
            return false;
        }
    }
}
