/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit
{
    /// <summary>
    /// Represents incoming request parameters from Wit.Ai.
    /// </summary>
    public struct ConduitParameterValue
    {
        /// <summary>
        /// The value of the parameter.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// The type of the parameter. If a type was not resolved to an existing type, this will be string by default.
        /// </summary>
        public Type DataType;

        public ConduitParameterValue(object value)
        {
            Value = value;
            DataType = value.GetType();
        }

        public ConduitParameterValue(object value, Type dataType)
        {
            Value = value;
            DataType = dataType;
        }
    }
}
