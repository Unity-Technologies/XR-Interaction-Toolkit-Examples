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
    [AttributeUsage(validOn:AttributeTargets.Field|AttributeTargets.Property, AllowMultiple = true)]
    public class JsonIgnoreAttribute : JsonPropertyAttribute
    {
        /// <summary>
        /// Constructor that sets property name to an empty string
        /// </summary>
        public JsonIgnoreAttribute() : base("", null) { }
    }
}
