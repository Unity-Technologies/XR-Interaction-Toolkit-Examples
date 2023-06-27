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
using Meta.Conduit;
using Meta.Conduit.Editor;
using Meta.WitAi;

namespace Lib.Conduit.Editor
{
    internal class SourceCodeMapper
    {
        /// <summary>
        /// Caches the type names and their corresponding source code file paths.
        /// </summary>
        private readonly Dictionary<string, string> _typeToFilePath = new Dictionary<string, string>();

        /// <summary>
        /// Gets the source file for the specified type.
        /// </summary>
        /// <returns>The file path if it exists. Null otherwise.</returns>
        public string GetSourceFilePathFromTypeName(string typeName, Manifest manifest, IAssemblyWalker assemblyWalker)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            if (_typeToFilePath.ContainsKey(typeName))
            {
                return _typeToFilePath[typeName];
            }

            var type = GetTypeFromTypeName(typeName, manifest, assemblyWalker);

            assemblyWalker.GetSourceCode(type, out var sourceCodeFile, out _);

            return _typeToFilePath[typeName] = sourceCodeFile;
        }

        private Type GetTypeFromTypeName(string typeName, Manifest manifest, IAssemblyWalker assemblyWalker)
        {
            var entity = manifest.Entities.FirstOrDefault(manifestEntity => manifestEntity.Name.Equals(typeName));

            if (entity == default)
            {
                VLog.D($"Could not find entity {typeName} in manifest");
                return null;
            }

            var qualifiedTypeName = entity.GetQualifiedTypeName();

            var allAssemblies = assemblyWalker.GetTargetAssemblies().ToList();

            if (!allAssemblies.Any())
            {
                return null;
            }

            var assemblies = allAssemblies
                .Where(a  => a.FullName == entity.Assembly).ToList();

            // If we can't find the assembly, the type may not be in the manifest yet. Try an exhaustive search.
            if (assemblies.Count < 1)
            {
                assemblies = allAssemblies;
            }

            var assembly = assemblies.First();

            var type = assembly.GetType(qualifiedTypeName);

            // If we can't find a full qualified name, fallback to simple nam.e
            return type ?? assembly.GetType(typeName);
        }
    }
}
