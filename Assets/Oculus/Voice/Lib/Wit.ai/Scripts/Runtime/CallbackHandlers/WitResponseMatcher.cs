/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Meta.WitAi.Attributes;
using Meta.WitAi.Data;
using Meta.WitAi.Json;
using Meta.WitAi.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Meta.WitAi.CallbackHandlers
{
    [AddComponentMenu("Wit.ai/Response Matchers/Response Matcher")]
    public class WitResponseMatcher : WitIntentMatcher
    {
        [FormerlySerializedAs("valuePaths")]
        [Header("Value Matching")]
#if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
#endif
        [SerializeField] public ValuePathMatcher[] valueMatchers;

        [Header("Output")]
#if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
#endif
        [SerializeField] private FormattedValueEvents[] formattedValueEvents;
        [SerializeField] private MultiValueEvent onMultiValueEvent = new MultiValueEvent();

        [TooltipBox("Triggered if the matching conditions did not match. The parameter will be the transcription that was received. This will only trigger if there were values for intents or entities, but those values didn't match this matcher.")]
        [SerializeField] private StringEvent onDidNotMatch = new StringEvent();

        [TooltipBox("Triggered if a request was checked and no intents were found. This will still trigger if entities match and only applies to intents. The parameter will be the transcription.")]
        [SerializeField] private StringEvent onOutOfDomain = new StringEvent();

        private static Regex valueRegex = new Regex(Regex.Escape("{value}"), RegexOptions.Compiled);

        // Handle validation
        protected override string OnValidateResponse(WitResponseNode response, bool isEarlyResponse)
        {
            // Return base
            string result = base.OnValidateResponse(response, isEarlyResponse);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
            // Only check value matches on early
            if (isEarlyResponse && !ValueMatches(response))
            {
                return "No value matches";
            }
            // Success
            return string.Empty;
        }
        // Ignore for mismatched intent
        protected override void OnResponseInvalid(WitResponseNode response, string error)
        {
            if (response.GetIntents().Length > 0 || response.EntityCount() > 0)
            {
                onDidNotMatch?.Invoke(response.GetTranscription());
            }

            if (response.GetIntents().Length == 0)
            {
                onOutOfDomain?.Invoke(response.GetTranscription());
            }
        }
        // Handle valid callback
        protected override void OnResponseSuccess(WitResponseNode response)
        {
            // Check value matches
            if (ValueMatches(response))
            {
                for (int j = 0; j < formattedValueEvents.Length; j++)
                {
                    var formatEvent = formattedValueEvents[j];
                    var result = formatEvent.format;
                    for (int i = 0; i < valueMatchers.Length; i++)
                    {
                        var reference = valueMatchers[i].Reference;
                        var value = reference.GetStringValue(response);
                        if (!string.IsNullOrEmpty(formatEvent.format))
                        {
                            if (!string.IsNullOrEmpty(value))
                            {
                                result = valueRegex.Replace(result, value, 1);
                                result = result.Replace("{" + i + "}", value);
                            }
                            else if (result.Contains("{" + i + "}"))
                            {
                                result = "";
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(result))
                    {
                        formatEvent.onFormattedValueEvent?.Invoke(result);
                    }
                }
            }
            else
            {
                onDidNotMatch?.Invoke(response.GetTranscription());
            }

            // Get all values & perform multi value event
            List<string> values = new List<string>();
            foreach (var matcher in valueMatchers)
            {
                // Add value
                var value = matcher.Reference.GetStringValue(response);
                values.Add(value);

                // Refresh confidence
                if (matcher.ConfidenceReference != null)
                {
                    float confidenceValue = ValueMatches(response, matcher)
                        ? matcher.ConfidenceReference.GetFloatValue(response)
                        : 0f;
                    RefreshConfidenceRange(confidenceValue, matcher.confidenceRanges, matcher.allowConfidenceOverlap);
                }
            }
            onMultiValueEvent.Invoke(values.ToArray());
        }

        private bool ValueMatches(WitResponseNode response)
        {
            bool matches = true;
            for (int i = 0; i < valueMatchers.Length && matches; i++)
            {
                matches &= ValueMatches(response, valueMatchers[i]);
            }
            return matches;
        }

        private bool ValueMatches(WitResponseNode response, ValuePathMatcher matcher)
        {
            var value = matcher.Reference.GetStringValue(response);
            bool result = !matcher.contentRequired || !string.IsNullOrEmpty(value);
            switch (matcher.matchMethod)
            {
                case MatchMethod.RegularExpression:
                    result &= Regex.Match(value, matcher.matchValue).Success;
                    break;
                case MatchMethod.Text:
                    result &= value == matcher.matchValue;
                    break;
                case MatchMethod.IntegerComparison:
                    result &= CompareInt(value, matcher);
                    break;
                case MatchMethod.FloatComparison:
                    result &= CompareFloat(value, matcher);
                    break;
                case MatchMethod.DoubleComparison:
                    result &= CompareDouble(value, matcher);
                    break;
            }
            return result;
        }

        private bool CompareDouble(string value, ValuePathMatcher matcher)
        {

            // This one is freeform based on the input so we will retrun false if it is not parsable
            if (!double.TryParse(value, out double dValue)) return false;

            // We will throw an exception if match value is not a numeric value. This is a developer
            // error.
            double dMatchValue = double.Parse(matcher.matchValue);

            switch (matcher.comparisonMethod)
            {
                case ComparisonMethod.Equals:
                    return Math.Abs(dValue - dMatchValue) < matcher.floatingPointComparisonTolerance;
                case ComparisonMethod.NotEquals:
                    return Math.Abs(dValue - dMatchValue) > matcher.floatingPointComparisonTolerance;
                case ComparisonMethod.Greater:
                    return dValue > dMatchValue;
                case ComparisonMethod.Less:
                    return dValue < dMatchValue;
                case ComparisonMethod.GreaterThanOrEqualTo:
                    return dValue >= dMatchValue;
                case ComparisonMethod.LessThanOrEqualTo:
                    return dValue <= dMatchValue;
            }

            return false;
        }

        private bool CompareFloat(string value, ValuePathMatcher matcher)
        {

            // This one is freeform based on the input so we will retrun false if it is not parsable
            if (!float.TryParse(value, out float dValue)) return false;

            // We will throw an exception if match value is not a numeric value. This is a developer
            // error.
            float dMatchValue = float.Parse(matcher.matchValue);

            switch (matcher.comparisonMethod)
            {
                case ComparisonMethod.Equals:
                    return Math.Abs(dValue - dMatchValue) <
                           matcher.floatingPointComparisonTolerance;
                case ComparisonMethod.NotEquals:
                    return Math.Abs(dValue - dMatchValue) >
                           matcher.floatingPointComparisonTolerance;
                case ComparisonMethod.Greater:
                    return dValue > dMatchValue;
                case ComparisonMethod.Less:
                    return dValue < dMatchValue;
                case ComparisonMethod.GreaterThanOrEqualTo:
                    return dValue >= dMatchValue;
                case ComparisonMethod.LessThanOrEqualTo:
                    return dValue <= dMatchValue;
            }

            return false;
        }

        private bool CompareInt(string value, ValuePathMatcher matcher)
        {

            // This one is freeform based on the input so we will retrun false if it is not parsable
            if (!int.TryParse(value, out int dValue)) return false;

            // We will throw an exception if match value is not a numeric value. This is a developer
            // error.
            int dMatchValue = int.Parse(matcher.matchValue);

            switch (matcher.comparisonMethod)
            {
                case ComparisonMethod.Equals:
                    return dValue == dMatchValue;
                case ComparisonMethod.NotEquals:
                    return dValue != dMatchValue;
                case ComparisonMethod.Greater:
                    return dValue > dMatchValue;
                case ComparisonMethod.Less:
                    return dValue < dMatchValue;
                case ComparisonMethod.GreaterThanOrEqualTo:
                    return dValue >= dMatchValue;
                case ComparisonMethod.LessThanOrEqualTo:
                    return dValue <= dMatchValue;
            }

            return false;
        }
    }

    [Serializable]
    public class MultiValueEvent : UnityEvent<string[]>
    {
    }

    [Serializable]
    public class ValueEvent : UnityEvent<string>
    { }

    [Serializable]
    public class FormattedValueEvents
    {
        [Tooltip("Modify the string output, values can be inserted with {value} or {0}, {1}, {2}")]
        public string format;
        public ValueEvent onFormattedValueEvent = new ValueEvent();
    }

    [Serializable]
    public class ValuePathMatcher
    {
        [Tooltip("The path to a value within a WitResponseNode")]
        public string path;
        [Tooltip("A reference to a wit value object")]
        public WitValue witValueReference;
        [Tooltip("Does this path need to have text in the value to be considered a match")]
        public bool contentRequired = true;
        [Tooltip("If set the match value will be treated as a regular expression.")]
        public MatchMethod matchMethod;
        [Tooltip("The operator used to compare the value with the match value. Ex: response.value > matchValue")]
        public ComparisonMethod comparisonMethod;
        [Tooltip("Value used to compare with the result when Match Required is set")]
        public string matchValue;

        [Tooltip("The variance allowed when comparing two floating point values for equality")]
        public double floatingPointComparisonTolerance = .0001f;

        [Tooltip("Confidence ranges are executed in order. If checked, all confidence values will be checked instead of stopping on the first one that matches.")]
        [SerializeField] public bool allowConfidenceOverlap;
        [Tooltip("The confidence levels to handle for this value.\nNOTE: The selected node must have a confidence sibling node.")]
        public ConfidenceRange[] confidenceRanges;

        private WitResponseReference pathReference;
        private WitResponseReference confidencePathReference;

        public WitResponseReference ConfidenceReference
        {
            get
            {
                if (null != confidencePathReference) return confidencePathReference;

                var confidencePath = Reference?.path;
                if (!string.IsNullOrEmpty(confidencePath))
                {
                    confidencePath = confidencePath.Substring(0, confidencePath.LastIndexOf("."));
                    confidencePath += ".confidence";
                    confidencePathReference = WitResultUtilities.GetWitResponseReference(confidencePath);
                }

                return confidencePathReference;
            }
        }
        public WitResponseReference Reference
        {
            get
            {
                if (witValueReference) return witValueReference.Reference;

                if (null == pathReference || pathReference.path != path)
                {
                    pathReference = WitResultUtilities.GetWitResponseReference(path);
                }

                return pathReference;
            }
        }
    }

    public enum ComparisonMethod
    {
        Equals,
        NotEquals,
        Greater,
        GreaterThanOrEqualTo,
        Less,
        LessThanOrEqualTo
    }

    public enum MatchMethod
    {
        None,
        Text,
        RegularExpression,
        IntegerComparison,
        FloatComparison,
        DoubleComparison
    }
}
