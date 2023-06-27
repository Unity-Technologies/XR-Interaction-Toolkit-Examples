/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using Meta.WitAi;
using UnityEditor.Compilation;
using Assembly = UnityEditor.Compilation.Assembly;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// This class is responsible for scanning assemblies for relevant Conduit data.
    /// </summary>
    internal class AssemblyWalker : IAssemblyWalker
    {
        /// <summary>
        /// The assembly that code not within an assembly is added to
        /// </summary>
        private const string DEFAULT_ASSEMBLY_NAME = "Assembly-CSharp";

        // All Conduit assemblies.
        private readonly Dictionary<string, IConduitAssembly> _assemblies = new Dictionary<string, IConduitAssembly>();

        private IEnumerable<Assembly> _compilationAssemblies;
        public HashSet<string> AssembliesToIgnore { get; set; } = new HashSet<string>();

        // The simple names of the assemblies to use in matching against compilation assemblies.
        private readonly HashSet<string> _shortAssemblyNamesToIgnore = new HashSet<string>();

        private IEnumerable<IConduitAssembly> ConduitAssemblies => _assemblies.Values;

        public AssemblyWalker(IList<IConduitAssembly> assemblies = null, IEnumerable<Assembly> compilationAssemblies = null)
        {
            IList<IConduitAssembly> conduitAssemblies;
            if (assemblies != null)
            {
                conduitAssemblies = assemblies;
            }
            else
            {
                conduitAssemblies = new List<IConduitAssembly>();
                var currentDomainConduitAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(assembly =>
                        assembly.IsDefined(typeof(ConduitAssemblyAttribute)) ||
                        string.Equals(DEFAULT_ASSEMBLY_NAME, assembly.GetName().Name));

                foreach (var conduitAssembly in currentDomainConduitAssemblies)
                {
                    conduitAssemblies.Add(new ConduitAssembly(conduitAssembly));
                }
            }

            foreach (var conduitAssembly in conduitAssemblies)
            {
                _assemblies.Add(conduitAssembly.FullName.Split(',').First(), conduitAssembly);
            }

            if (compilationAssemblies != null)
            {
                _compilationAssemblies = compilationAssemblies;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IConduitAssembly> GetAllAssemblies()
        {
            return ConduitAssemblies;
        }

        /// <inheritdoc/>
        public IEnumerable<IConduitAssembly> GetTargetAssemblies()
        {
            if (AssembliesToIgnore != null && AssembliesToIgnore.Any()) {
                return GetAllAssemblies().Where(assembly => !AssembliesToIgnore.Contains(assembly.FullName));
            }
            
            return GetAllAssemblies();
        }

        /// <inheritdoc/>
        public IEnumerable<Assembly> GetCompilationAssemblies(AssembliesType assembliesType)
        {
            GenerateExcludedAssembliesShortNames();
            
            if (_compilationAssemblies == null)
            {
                _compilationAssemblies = CompilationPipeline.GetAssemblies(assembliesType);
            }
            return _compilationAssemblies.Where(assembly => !_shortAssemblyNamesToIgnore.Contains(assembly.name));
        }

        public bool GetSourceCode(Type type, out string sourceCodeFile, out bool singleUnit)
        {
            if (type == null)
            {
                throw new ArgumentException("Type cannot be null");
            }

            foreach (var assembly in GetCompilationAssemblies(AssembliesType.Player))
            {
                if (!_assemblies.ContainsKey(assembly.name))
                {
                    continue;
                }

                if (GetSourceCodeFromAssembly(assembly, type, out sourceCodeFile, out singleUnit))
                {
                    if (!singleUnit)
                    {
                        VLog.W($"Type {type} is not in a separate file.");
                    }

                    return true;
                }
            }

            VLog.W($"Failed to find source code for enum {type}");
            sourceCodeFile = string.Empty;
            singleUnit = false;
            return false;
        }
        
        private void GenerateExcludedAssembliesShortNames()
        {
            _shortAssemblyNamesToIgnore.Clear();
            foreach (var assemblyName in AssembliesToIgnore)
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    VLog.W("Attempting to exclude invalid assembly name.");
                    continue;
                }

                var simpleName = assemblyName.Split(',').First();
                _shortAssemblyNamesToIgnore.Add(simpleName);
            }
        }

        private bool GetSourceCodeFromAssembly(Assembly assembly, Type type, out string sourceCodeFile, out bool singleUnit)
        {
            // TODO: Cache code files.
            var defaultFileName = GetDefaultFileName(type);

            foreach (var sourceFile in assembly.sourceFiles)
            {
                if (!sourceFile.EndsWith(defaultFileName))
                {
                    continue;
                }

                string sourceCode = "";
                try
                {
                    sourceCode = File.ReadAllText(sourceFile);
                }
                catch (Exception e)
                {
                    VLog.D($"Failed to read file {sourceFile}.\n{e}");
                    sourceCodeFile = string.Empty;
                    singleUnit = false;
                    return false;
                }


                if (!ContainsType(sourceCode, type))
                {
                    continue;
                }

                singleUnit = IsSingleUnitSourceCode(sourceCode);

                sourceCodeFile = sourceFile;
                return true;
            }

            sourceCodeFile = string.Empty;
            singleUnit = false;
            return false;
        }

        /// <summary>
        /// Returns true if the code contains only a single unit (enum/class/struct) defined.
        /// This is not 100% accurate as it relies on simple code search so may return false positives.
        /// This checks only for classes, structs, and enums.  
        /// </summary>
        /// <returns></returns>
        private bool IsSingleUnitSourceCode(string sourceCode)
        {
            // This matches enums, classes and structs including their identifiers and nested braces (for scopes)
            var codeBlockPattern = @"(enum|class|struct)\s\w+[\n\r\s]*\{(?>\{(?<c>)|[^{}]+|\}(?<-c>))*(?(c)(?!))\}";

            return Regex.Matches(sourceCode, codeBlockPattern).Count == 1;
        }

        private bool ContainsType(string sourceCode, Type type)
        {
            var pattern = $"(enum|class|struct)\\s{type.Name}";

            return Regex.IsMatch(sourceCode, pattern);
        }

        private string GetDefaultFileName(Type type)
        {
            return $"{type.Name}.cs";
        }
    }
}
