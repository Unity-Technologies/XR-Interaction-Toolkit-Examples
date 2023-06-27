/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Validates whether a parameter type is supported or not.
    /// </summary>
    internal interface IParameterValidator
    {
        /// <summary>
        /// Tests if a parameter type can be supplied directly to a callback method from.
        /// </summary>
        /// <param name="type">The data type.</param>
        /// <returns>True if the parameter type is supported. False otherwise.</returns>
        bool IsSupportedParameterType(Type type);
    }
}
