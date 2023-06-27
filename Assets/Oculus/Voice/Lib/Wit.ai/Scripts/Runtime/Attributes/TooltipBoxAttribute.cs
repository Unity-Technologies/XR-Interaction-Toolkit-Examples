/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.WitAi.Attributes
{
    public class TooltipBoxAttribute : PropertyAttribute
    {
        public string Text { get; private set; }

        public TooltipBoxAttribute(string text)
        {
            Text = text;
        }
    }
}
