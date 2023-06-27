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
    /// Class for custom json serializing
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Method for performing custom Json object serialization
        /// </summary>
        /// <param name="jsonObject">Initial json object to be encoded</param>
        /// <returns>True if successfully encoded</returns>
        bool SerializeObject(WitResponseClass jsonObject);
    }
}
