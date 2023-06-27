/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Wrapper for assemblies to provide convenience methods and abstract from CLR.
    /// </summary>
    internal interface IConduitAssembly
    {
        /// <summary>
        /// The full assembly name.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Returns all types in the assembly that are enums.
        /// </summary>
        /// <returns>The enum types.</returns>
        IEnumerable<Type> GetEnumTypes();

        /// <summary>
        /// Returns all the methods in the assembly.
        /// </summary>
        /// <returns>The methods.</returns>
        IEnumerable<MethodInfo> GetMethods();

        /// <summary>
        /// Returns the type of the given name from the assembly.
        /// </summary>
        /// <param name="name">The type name.</param>
        /// <returns>The type.</returns>
        Type GetType(string name);
    }
}
