/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Attributes;
using UnityEngine;
using Utilities;

namespace Meta.WitAi.Utilities
{
    [AddComponentMenu("Wit.ai/Utilities/Conversions/String to String")]
    public class StringToStringEvent : MonoBehaviour
    {
        [Tooltip("The string format string that will be used to reformat input strings. Ex: I don't know how to respond to {0}")]
        [SerializeField] private string _format;

        [Space(WitRuntimeStyles.HeaderPaddingTop)]
        [TooltipBox("Triggered when FormatString(float) is called. The string in this event will be formatted based on the format field.")]
        [SerializeField] public StringEvent onStringEvent = new StringEvent();

        /// <summary>
        /// Trigger an onStringEvent with a provided format.
        /// </summary>
        /// <param name="format">The string format to use in the event</param>
        /// <param name="value">The value that will get populated in {0} in the format string.</param>
        public void FormatString(string format, string value)
        {
            if (string.IsNullOrEmpty(format))
            {
                onStringEvent?.Invoke(value);
            }
            else
            {
                onStringEvent?.Invoke(string.Format(format, value));
            }
        }

        /// <summary>
        /// Trigger an onStringEvent with the built in format value
        /// </summary>
        /// <param name="value">The text to insert into {0} in the format value.</param>
        public void FormatString(string value)
        {
            FormatString(_format, value);
        }
    }
}
