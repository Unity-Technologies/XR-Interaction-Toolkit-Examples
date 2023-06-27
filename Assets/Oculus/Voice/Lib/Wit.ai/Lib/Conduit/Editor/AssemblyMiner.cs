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
using System.Text;
using Meta.WitAi;
using Meta.WitAi.Data.Info;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Mines assemblies for callback methods and entities.
    /// </summary>
    internal class AssemblyMiner : IAssemblyMiner
    {
        /// <summary>
        /// Validates that parameters are compatible.
        /// </summary>
        private readonly IParameterValidator _parameterValidator;

        /// <summary>
        /// Set to true once the miner is initialized. No interactions with the class should be allowed before then.
        /// </summary>
        private bool _initialized = false;

        /// <inheritdoc/>
        public Dictionary<string, int> SignatureFrequency { get; private set; } = new Dictionary<string, int>();

        /// <inheritdoc/>
        public Dictionary<string, int> IncompatibleSignatureFrequency { get; private set; } =
            new Dictionary<string, int>();

        /// <summary>
        /// Initializes the class with a target assembly.
        /// </summary>
        /// <param name="parameterValidator">The parameter validator.</param>
        public AssemblyMiner(IParameterValidator parameterValidator)
        {
            this._parameterValidator = parameterValidator;
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            SignatureFrequency = new Dictionary<string, int>();
            IncompatibleSignatureFrequency = new Dictionary<string, int>();
            _initialized = true;
        }

        /// <inheritdoc/>
        public List<ManifestEntity> ExtractEntities(IConduitAssembly assembly)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Assembly Miner not initialized");
            }

            var entities = new List<ManifestEntity>();
            var enums = assembly.GetEnumTypes();
            foreach (var enumType in enums)
            {
                Array enumValues;
                try
                {
                    if (enumType.GetCustomAttributes(typeof(ConduitEntityAttribute), false).Length == 0)
                    {
                        // This is not a tagged entity.
                        // TODO: In these cases we should only include the enum if it's referenced by any of the actions.
                    }

                    enumValues = enumType.GetEnumValues();
                }
                catch (Exception e)
                {
                    VLog.W($"Failed to get enumeration values.\nEnum: {enumType}\n{e}");
                    continue;
                }

                var entity = new ManifestEntity
                {
                    ID = enumType.Name,
                    Type = "Enum",
                    Namespace = enumType.Namespace,
                    Name = enumType.Name,
                    Assembly = assembly.FullName
                };

                var values = new List<WitKeyword>();

                foreach (var enumValue in enumValues)
                {
                    var synonyms = new List<string>();
                    var attribute = GetAttribute<ConduitValueAttribute>(enumValue);
                    if (attribute != null)
                    {
                        foreach (var alias in attribute.Aliases)
                        {
                            synonyms.Add(alias);
                        }
                    }

                    if (enumValue == null)
                    {
                        VLog.E("Unexpected null enum value");
                        continue;
                    }

                    values.Add(new WitKeyword(enumValue.ToString(), synonyms));
                }

                entity.Values = values;
                entities.Add(entity);
            }

            return entities;
        }

        private static T GetAttribute<T>(object enumValue) where T : Attribute
        {
            var type = enumValue.GetType();
            var memberInfos = type.GetMember(enumValue.ToString());
            if (memberInfos.Length == 0)
            {
                return null;
            }

            var attributes = memberInfos.First().GetCustomAttributes(typeof(ConduitValueAttribute), false);
            if (attributes.Length == 0)
            {
                return null;
            }

            return attributes.First() as T;
        }

        private ManifestParameter GetManifestParameters(ParameterInfo parameter, Type attributeType, string actionID)
        {
            
            List<string> aliases;
            List<string> examples;

            if (parameter.GetCustomAttributes(attributeType, false).Length > 0)
            {
                var parameterAttribute =
                    parameter.GetCustomAttributes(attributeType, false).First() as
                        ConduitParameterAttribute;
                aliases = parameterAttribute.Aliases;
                examples = parameterAttribute.Examples;
            }
            else
            {
                aliases = new List<string>();
                examples = new List<string>();
            }

            var snakeCaseName = ConduitUtilities.DelimitWithUnderscores(parameter.Name).ToLower()
                .TrimStart('_');
            var snakeCaseAction = actionID.Replace('.', '_');

            var manifestParameter = new ManifestParameter
            {
                Name = ConduitUtilities.SanitizeName(parameter.Name),
                InternalName = parameter.Name,
                QualifiedTypeName = parameter.ParameterType.FullName,
                TypeAssembly = parameter.ParameterType.Assembly.FullName,
                Aliases = aliases,
                Examples = examples,
                QualifiedName = $"{snakeCaseAction}_{snakeCaseName}"
            };

            return manifestParameter;
        }
        private List<ManifestAction> ExtractActionsInternal(Type attributeType, IConduitAssembly assembly)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Assembly Miner not initialized");
            }

            var methods = assembly.GetMethods();

            var actions = new List<ManifestAction>();

            foreach (var method in methods)
            {
                if (method == null)
                {
                    VLog.E($"Found a null method in assembly: {assembly.FullName}");
                    continue;
                }

                if (method.DeclaringType == null)
                {
                    VLog.E($"Method {method.Name} in assembly {assembly.FullName} had null declaring type");
                    continue;
                }
                
                
                var attributes = method.GetCustomAttributes(typeof(ConduitActionAttribute), false);
                if (attributes.Length == 0)
                {
                    continue;
                }

                var actionAttribute = attributes.First() as ConduitActionAttribute;
                var actionName = actionAttribute?.Intent;
                if (string.IsNullOrEmpty(actionName))
                {
                    actionName = $"{method.Name}";
                }

                var parameters = new List<ManifestParameter>();

                var action = new ManifestAction()
                {
                    ID = $"{method.DeclaringType.FullName}.{method.Name}",
                    Name = actionName,
                    Assembly = assembly.FullName,
                };

                var compatibleParameters = true;

                var signature = GetMethodSignature(method);

                // We track this first regardless of whether or not Conduit supports it to identify gaps.
                SignatureFrequency.TryGetValue(signature, out var currentFrequency);
                SignatureFrequency[signature] = currentFrequency + 1;

                foreach (var parameter in method.GetParameters())
                {
                    var supported = _parameterValidator.IsSupportedParameterType(parameter.ParameterType);
                    if (!supported)
                    {
                        compatibleParameters = false;
                        VLog.W($"Conduit does not currently support parameter type: {parameter.ParameterType}");
                        continue;
                    }
                    parameters.Add(GetManifestParameters(parameter, attributeType, action.ID));
                }

                if (compatibleParameters)
                {
                    action.Parameters = parameters;
                    actions.Add(action);
                }
                else
                {
                    VLog.W($"{method} has Conduit-Incompatible Parameters");
                    IncompatibleSignatureFrequency.TryGetValue(signature, out currentFrequency);
                    IncompatibleSignatureFrequency[signature] = currentFrequency + 1;
                }
            }

            return actions;
        }

        private List<ManifestErrorHandler> ExtractErrorHandlersInternal(Type attributeType, IConduitAssembly assembly)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Assembly Miner not initialized");
            }

            var methods = assembly.GetMethods();

            var actions = new List<ManifestErrorHandler>();

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0)
                {
                    continue;
                }
                
                var parameters = new List<ManifestParameter>();

                var action = new ManifestErrorHandler()
                {
                    ID = $"{method.DeclaringType.FullName}.{method.Name}",
                    // Name = actionName,
                    Assembly = assembly.FullName,
                    Name = method.Name
                };

                var compatibleParameters = true;

                var signature = GetMethodSignature(method);

                // We track this first regardless of whether or not Conduit supports it to identify gaps.
                SignatureFrequency.TryGetValue(signature, out var currentFrequency);
                SignatureFrequency[signature] = currentFrequency + 1;

                var methodParameters = method.GetParameters();
                if (methodParameters.Length < 2)
                {
                    VLog.E("Not enough parameters provided for error handler " + method.Name);
                    continue;
                }
                if (methodParameters[0].ParameterType != typeof(string))
                {
                    VLog.E("First parameter must be a string for error handler " + method.Name);
                    continue;
                }
                if (methodParameters[1].ParameterType != typeof(Exception))
                {
                    VLog.E("Second parameter must be an exception for error handler " + method.Name);
                    continue;
                }
                
                foreach (var parameter in methodParameters)
                {
                    var supported = _parameterValidator.IsSupportedParameterType(parameter.ParameterType);
                    if (!supported)
                    {
                        compatibleParameters = false;
                        VLog.W($"Conduit does not currently support parameter type: {parameter.ParameterType}");
                        continue;
                    }
                    
                    parameters.Add(GetManifestParameters(parameter, attributeType, action.ID));
                }
               
                if (compatibleParameters)
                {
                    action.Parameters = parameters;
                    actions.Add(action);
                }
                else
                {
                    VLog.W($"{method} has Conduit-Incompatible Parameters");
                    IncompatibleSignatureFrequency.TryGetValue(signature, out currentFrequency);
                    IncompatibleSignatureFrequency[signature] = currentFrequency + 1;
                }
            }

            return actions;
        }

        /// <inheritdoc/>
        public List<ManifestAction> ExtractActions(IConduitAssembly assembly)
        {
            return ExtractActionsInternal(typeof(ConduitActionAttribute), assembly);
        }

        public List<ManifestErrorHandler> ExtractErrorHandlers(IConduitAssembly assembly)
        {
            return ExtractErrorHandlersInternal(typeof(HandleEntityResolutionFailureAttribute), assembly);
        }

        /// <summary>
        /// Generate a method signature summary that ignores method and parameter names but keeps types.
        /// For example:
        /// string F(int a, int b, float c) => string!int:2,float:1
        /// static string F(int a, int b, float c) => #string!int:2,float:1
        /// </summary>
        /// <param name="methodInfo">The method we are capturing.</param>
        /// <returns>A string representing the relevant data types.</returns>
        private string GetMethodSignature(MethodInfo methodInfo)
        {
            var sb = new StringBuilder();
            if (methodInfo.IsStatic)
            {
                sb.Append('#');
            }

            sb.Append(methodInfo.ReturnType);
            sb.Append('!');
            var parameters = new SortedDictionary<string, int>();
            foreach (var parameter in methodInfo.GetParameters())
            {
                parameters.TryGetValue(parameter.ParameterType.Name, out var currentFrequency);
                parameters[parameter.ParameterType.Name] = currentFrequency + 1;
            }

            var first = true;
            foreach (var parameter in parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(',');
                }

                sb.Append(parameter.Key);
                sb.Append(':');
                sb.Append(parameter.Value);
            }

            return sb.ToString();
        }
    }
}
