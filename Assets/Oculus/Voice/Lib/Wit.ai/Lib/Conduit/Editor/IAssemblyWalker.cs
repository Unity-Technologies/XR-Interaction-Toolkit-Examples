/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor.Compilation;

namespace Meta.Conduit.Editor
{
    internal interface IAssemblyWalker
    {
        /// <summary>
        /// Filter out assemblies with specified names from GetTargetAssemblies() output.
        /// </summary>
        HashSet<string> AssembliesToIgnore { get; set; }

        /// <summary>
        /// Returns a list of all assemblies that are marked with the <see cref="ConduitAssemblyAttribute"/> attribute.
        /// </summary>
        /// <returns>The list of assemblies.</returns>
        IEnumerable<IConduitAssembly> GetAllAssemblies();

        /// <summary>
        /// Returns a list of all assemblies that should be processed.
        /// This currently selects assemblies that are marked with the <see cref="ConduitAssemblyAttribute"/> attribute.
        /// </summary>
        /// <returns>The list of assemblies.</returns>
        IEnumerable<IConduitAssembly> GetTargetAssemblies();

        /// <summary>
        /// Returns a list of assemblies that Unity will build for Edit or Run time.
        /// </summary>
        /// <returns>The list of assemblies in the compilation pipeline.</returns>
        IEnumerable<Assembly> GetCompilationAssemblies(AssembliesType assembliesType);

        /// <summary>
        /// Returns the file path containing the specified type.
        /// </summary>
        /// <param name="type">The type to look for.</param>
        /// <param name="sourceCodeFile">The full filepath.</param>
        /// <param name="singleUnit">
        /// True if the type is the only type defined in the file.
        /// False if there are other types in the same file.
        /// </param>
        /// <returns>True if the type was found. False otherwise.</returns>
        bool GetSourceCode(Type type, out string sourceCodeFile, out bool singleUnit);
    }
}
