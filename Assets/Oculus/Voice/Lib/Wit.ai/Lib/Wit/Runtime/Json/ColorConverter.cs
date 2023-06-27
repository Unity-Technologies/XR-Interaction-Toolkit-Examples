/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Json
{
    public class ColorConverter : JsonConverter
    {
        // Reads & Writes
        public override bool CanRead => true;
        public override bool CanWrite => true;

        // Can convert to color
        public override bool CanConvert(Type objectType)
        {
            return typeof(Color) == objectType;
        }

        // Decode into color
        public override object ReadJson(WitResponseNode serializer, Type objectType, object existingValue)
        {
            if (ColorUtility.TryParseHtmlString(serializer.Value, out var result))
            {
                return result;
            }
            return existingValue;
        }

        // Decode from color
        public override WitResponseNode WriteJson(object existingValue)
        {
            Color result = (Color)existingValue;
            return new WitResponseData(ColorUtility.ToHtmlStringRGBA(result));
        }
    }
}

