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
    /// Filters out parameters of specific types.
    /// </summary>
    internal interface IParameterFilter
    {
        /// <summary>
        /// Tests if a parameter type should be filtered out.
        /// </summary>
        /// <param name="type">The data type.</param>
        /// <returns>True if the parameter type should be filtered out. False otherwise.</returns>
        bool ShouldFilterOut(Type type);

        /// <summary>
        /// Tests if a parameter type should be filtered out.
        /// </summary>
        /// <param name="typeName">The name of the data type.</param>
        /// <returns>True if the parameter type should be filtered out. False otherwise.</returns>
        bool ShouldFilterOut(string typeName);

    }
}
