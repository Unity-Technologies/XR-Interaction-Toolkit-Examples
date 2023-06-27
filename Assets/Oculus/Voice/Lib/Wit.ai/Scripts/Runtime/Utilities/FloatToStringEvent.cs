/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Attributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Utilities;

namespace Meta.WitAi.Utilities
{
    [AddComponentMenu("Wit.ai/Utilities/Conversions/Float to String")]
    public class FloatToStringEvent : MonoBehaviour
    {
        [FormerlySerializedAs("format")]
        [Tooltip("The format value to be used on the float")]
        [SerializeField] private string _floatFormat;
        [Tooltip("The format of the string itself. {0} will represent the float value provided")]
        [SerializeField] private string _stringFormat;

        [Space(WitRuntimeStyles.HeaderPaddingTop)]
        [TooltipBox("Triggered when ConvertFloatToString(float) is called. The string in this event will be formatted based on the format fields.")]
        [SerializeField] private StringEvent onFloatToString = new StringEvent();

        /// <summary>
        /// Converts a float to a string using the component format values and emits an onFloatToString event.
        /// </summary>
        /// <param name="value"></param>
        public void ConvertFloatToString(float value)
        {
            string floatStringValue;
            if (string.IsNullOrEmpty(_floatFormat))
            {
                floatStringValue = value.ToString();
            }
            else
            {
                floatStringValue = value.ToString(_floatFormat);
            }

            if (string.IsNullOrEmpty(_stringFormat))
            {
                onFloatToString?.Invoke(floatStringValue);
            }
            else
            {
                onFloatToString?.Invoke(string.Format(_stringFormat, floatStringValue));
            }
        }
    }

    [Serializable]
    public class StringEvent : UnityEvent<string> {}
}
