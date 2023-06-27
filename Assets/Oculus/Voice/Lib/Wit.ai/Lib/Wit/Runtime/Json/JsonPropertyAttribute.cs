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
    public class JsonPropertyAttribute : Attribute
    {
        /// <summary>
        /// Property name used to convert json
        /// </summary>
        public string PropertyName { get; private set; }
        /// <summary>
        /// Default value for property
        /// </summary>
        public object DefaultValue { get; private set; }

        /// <summary>
        /// Constructor that sets property name
        /// </summary>
        /// <param name="propertyName">Name to be read from json</param>
        public JsonPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
            DefaultValue = null;
        }
        /// <summary>
        /// Construct for property name & default value
        /// </summary>
        /// <param name="propertyName">Name to be read from json</param>
        /// <param name="defaultValue">Default value when the json object is missing</param>
        public JsonPropertyAttribute(string propertyName, object defaultValue)
        {
            PropertyName = propertyName;
            DefaultValue = defaultValue;
        }
    }
}
