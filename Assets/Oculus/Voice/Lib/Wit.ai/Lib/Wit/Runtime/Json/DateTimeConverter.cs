/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.WitAi.Json
{
    public class DateTimeConverter : JsonConverter
    {
        // Reads & Writes
        public override bool CanRead => true;
        public override bool CanWrite => true;

        // Can convert to date time
        public override bool CanConvert(Type objectType)
        {
            return typeof(DateTime) == objectType;
        }

        // Decode into date
        public override object ReadJson(WitResponseNode serializer, Type objectType, object existingValue)
        {
            if (DateTime.TryParse(serializer.Value, out var result))
            {
                return result;
            }
            return existingValue;
        }

        // Decode from date
        public override WitResponseNode WriteJson(object existingValue)
        {
            DateTime result = (DateTime)existingValue;
            return new WitResponseData($"{result.ToLongDateString()} {result.ToLongTimeString()}");
        }
    }
}

