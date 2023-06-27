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
    public abstract class JsonConverter
    {
        /// <summary>
        /// Whether this script overrides read
        /// </summary>
        public virtual bool CanRead { get; }

        /// <summary>
        /// Whether this script overrides write
        /// </summary>
        public virtual bool CanWrite { get; }

        /// <summary>
        /// Whether this type can convert the specified type
        /// </summary>
        public abstract bool CanConvert(Type objectType);

        /// <summary>
        /// Converts serialized token into desired value
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public virtual object ReadJson(WitResponseNode serializer, Type objectType, object existingValue)
        {
            return existingValue;
        }

        /// <summary>
        /// Returns json token from existing value
        /// </summary>
        /// <param name="deserializer"></param>
        /// <param name="existingValue"></param>
        public virtual WitResponseNode WriteJson(object existingValue)
        {
            return null;
        }
    }
}
