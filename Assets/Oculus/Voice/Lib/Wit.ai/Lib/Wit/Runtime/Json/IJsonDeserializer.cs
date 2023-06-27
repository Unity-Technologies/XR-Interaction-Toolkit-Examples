/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi.Json
{
    /// <summary>
    /// Class for custom json deserialization
    /// </summary>
    public interface IJsonDeserializer
    {
        /// <summary>
        /// Decode method for json deserialization
        /// </summary>
        /// <param name="jsonObject">Json resultant object class</param>
        /// <returns>True if successfully deserialized</returns>
        bool DeserializeObject(WitResponseClass jsonObject);
    }
}
