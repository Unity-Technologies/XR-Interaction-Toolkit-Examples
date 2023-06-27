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
using System.Reflection;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Wraps an assembly and provides access to Conduit-relevant details.
    /// </summary>
    internal class ConduitAssembly : IConduitAssembly
    {
        /// <summary>
        /// The assembly this class wraps.
        /// </summary>
        private readonly Assembly _assembly;
        
        /// <summary>
        /// Initializes the class with a target assembly.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        public ConduitAssembly(Assembly assembly)
        {
            this._assembly = assembly;
        }
        
        public string FullName => this._assembly.FullName;

        public IEnumerable<Type> GetEnumTypes()
        {
            return this._assembly.GetTypes().Where(p => p.IsEnum);
        }

        public IEnumerable<MethodInfo> GetMethods()
        {
            return this._assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
        }

        public Type GetType(string name)
        {
            return this._assembly.GetType(name);
        }
    }
}
