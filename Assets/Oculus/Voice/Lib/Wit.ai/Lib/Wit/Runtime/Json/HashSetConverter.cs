/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.WitAi.Json
{
    public class HashSetConverter<T> : JsonConverter
    {
        // Reads & Writes
        public override bool CanRead => true;
        public override bool CanWrite => true;

        // Can convert to color
        public override bool CanConvert(Type objectType)
        {
            return typeof(HashSet<T>) == objectType;
        }

        // Decode into color
        public override object ReadJson(WitResponseNode serializer, Type objectType, object existingValue)
        {
            var array = serializer.AsArray;

            var result = new HashSet<T>();

            foreach (WitResponseNode node in array)
            {
                result.Add(node.Cast<T>());
            }

            return result;
        }

        // Decode from color
        public override WitResponseNode WriteJson(object existingValue)
        {
            var responseArray = new WitResponseArray();
            var hashSet = existingValue as HashSet<T>;
            if (hashSet == null)
            {
                return responseArray;
            }

            //var list = JsonConvert.SerializeObject(hashSet.ToList());
            foreach (var item in hashSet)
            {
                responseArray.Add(new WitResponseData(item.ToString()));
            }

            return responseArray;
        }
    }

}
