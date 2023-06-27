/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.WitAi.Utilities
{
    public class DynamicRangeAttribute : PropertyAttribute
    {
        public string RangeProperty { get; private set; }
        public float DefaultMin { get; private set; }
        public float DefaultMax { get; private set; }

        public DynamicRangeAttribute(string rangeProperty, float defaultMin = float.MinValue, float defaultMax = float.MaxValue)
        {
            DefaultMin = defaultMin;
            DefaultMax = defaultMax;
            RangeProperty = rangeProperty;
        }
    }
}
