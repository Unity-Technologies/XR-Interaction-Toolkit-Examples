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
using Meta.WitAi.Json;
using Meta.Conduit;

namespace Meta.Conduit
{
    /// <summary>
    /// Resolves parameters for invoking callbacks. This can be derived to support additional parameter types.
    /// </summary>
    internal class ParameterProvider : IParameterProvider
    {
        public const string WitResponseNodeReservedName = "@WitResponseNode";
        public const string VoiceSessionReservedName = "@VoiceSession";
        
        /// <summary>
        /// Maps the parameters to their supplied values.
        /// The keys are normalized to lowercase.
        /// </summary>
        protected readonly Dictionary<string, object> ActualParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maps internal parameter names (in code) to fully qualified parameter names (roles/slots).
        /// The keys are normalized to lowercase.
        /// </summary>
        private readonly Dictionary<string, string> _parameterToRoleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Maps types to the list of parameters of that type. This is used as a cache to avoid repeated type lookups.
        /// </summary>
        private readonly Dictionary<Type, List<string>> _parametersOfType = new Dictionary<Type, List<string>>();

        /// <summary>
        /// Maps reserved parameter types to their reserved names.
        /// </summary>
        private readonly Dictionary<Type, string> _specializedParameters = new Dictionary<Type, string>();
        
        /// <summary>
        /// Maps Wit.Ai type names to native types that support them starting with the preferred data type.
        /// </summary>
        private static readonly Dictionary<string, List<Type>> BuiltInTypes = new Dictionary<string, List<Type>>()
        {
            { "wit$age_of_person", new List<Type>{typeof(int), typeof(short), typeof(long), typeof(float), typeof(double), typeof(decimal) }},
            { "wit$amount_of_money", new List<Type>{typeof(decimal), typeof(float), typeof(double), typeof(int)}},
            { "wit$datetime", new List<Type>{typeof(DateTime)}},
            { "wit$distance", new List<Type>{typeof(decimal), typeof(float), typeof(double), typeof(int)}},
            { "wit$duration", new List<Type>{typeof(TimeSpan), typeof(float), typeof(double), typeof(int), typeof(decimal)}},
            { "wit$number", new List<Type>{typeof(int), typeof(long), typeof(short), typeof(float), typeof(double), typeof(decimal) }},
            { "wit$ordinal", new List<Type>{typeof(int), typeof(long), typeof(short) }},
            { "wit$quantity", new List<Type>{typeof(int), typeof(long), typeof(short), typeof(float), typeof(double), typeof(decimal) }},
            { "wit$temperature", new List<Type>{typeof(decimal), typeof(float), typeof(double), typeof(int), typeof(short), typeof(long)}},
            { "wit$volume", new List<Type>{typeof(int), typeof(long), typeof(short), typeof(float), typeof(double), typeof(decimal) }}
        };

        /// <summary>
        /// Custom types defined locally.
        /// </summary>
        private readonly Dictionary<string, Type> _customTypes = new Dictionary<string, Type>();
        
        /// <summary>
        /// The list of the names of all parameters in the provider. 
        /// </summary>
        public List<string> AllParameterNames => this.ActualParameters.Keys.ToList();

        /// <summary>
        /// Add a custom known type (typically enum) to the provider.
        /// This should be called BEFORE calling any of the population methods.
        /// </summary>
        /// <param name="name">The internal name of the type.</param>
        /// <param name="type">The data type.</param>
        public void AddCustomType(string name, Type type)
        {
            _customTypes[name] = type;
        }
        
        /// <summary>
        /// Explicitly adds, or replaces, a parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        public void AddParameter(string parameterName, object value)
        {
            ActualParameters[parameterName] = value;
        }
        
        /// <summary>
        /// Extracts Conduit parameters from a Wit.Ai response.
        /// </summary>
        /// <param name="responseNode">The response node from Wit.Ai</param>
        /// <returns>A dictionary where the parameter names are keys and they </returns>
        public void PopulateParametersFromNode(WitResponseNode responseNode)
        {
            _parametersOfType.Clear();

            var parameters = new Dictionary<string, ConduitParameterValue>();
            foreach (var entity in responseNode.AsObject["entities"].Childs)
            {
                var parameterName = entity[0]["role"].Value;
                var parameterValue = entity[0]["value"].Value;
                var parameterTypes = GetParameterTypes(entity[0]["name"].Value, parameterValue).ToList();

                foreach (var parameterType in parameterTypes)
                {
                    if (!_parametersOfType.ContainsKey(parameterType))
                    {
                        _parametersOfType.Add(parameterType, new List<string>());
                    }

                    _parametersOfType[parameterType].Add(parameterName);
                }

                parameters.Add(parameterName, new ConduitParameterValue(parameterValue, parameterTypes.First()));
            }
            parameters.Add(WitResponseNodeReservedName, new ConduitParameterValue(responseNode, typeof(WitResponseNode)));
            
            PopulateParameters(parameters);
        }
        
        /// <summary>
        /// Registers a certain keyword as reserved for a specialized parameter.
        /// </summary>
        /// <param name="reservedParameterName">The name of the specialized parameter. For example @WitResponseNode</param>
        /// <param name="parameterType">The data type of the parameter</param>
        public void SetSpecializedParameter(string reservedParameterName, Type parameterType)
        {
            _specializedParameters[parameterType] = reservedParameterName.ToLower();
        }

        /// <summary>
        /// Populates the parameters.
        /// Must be called after all parameters have been obtained and mapped but before any are read.
        /// </summary>
        public void PopulateParameters(Dictionary<string, ConduitParameterValue> actualParameters)
        {
            ActualParameters.Clear();
            foreach (var actualParameter in actualParameters)
            {
                ActualParameters[actualParameter.Key] = actualParameter.Value.Value;
            }
        }

        /// <summary>
        /// Populates the roles mappings between actual parameters and their roles..
        /// Must be called after all parameters have been populated using PopulateParameters but before any are read.
        /// </summary>
        /// <param name="parameterToRoleMap">
        /// Keys are normalized lowercase internal (code) names.
        /// Values are fully qualified parameter names (roles)
        /// </param>
        public void PopulateRoles(Dictionary<string, string> parameterToRoleMap)
        {
            _parameterToRoleMap.Clear();
            foreach (var entry in parameterToRoleMap)
            {
                _parameterToRoleMap[entry.Key.ToLower()] = entry.Value;
            }
        }

        /// <summary>
        /// Returns true if a parameter with the specified name can be provided.
        /// </summary>
        /// <param name="parameter">The name of the parameter.</param>
        /// <param name="log">The log to write to.</param>
        /// <returns>True if a parameter with the specified name can be provided.</returns>
        public bool ContainsParameter(ParameterInfo parameter, StringBuilder log)
        {
            if (SupportedSpecializedParameter(parameter))
            {
                return true;
            }
            if (!ActualParameters.ContainsKey(parameter.Name))
            {
                log.AppendLine($"\tParameter '{parameter.Name}' not sent in invoke");
                return false;
            }
            if (!_parameterToRoleMap.ContainsKey(parameter.Name))
            {
                log.AppendLine($"\tParameter '{parameter.Name}' not found in role map");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Provides the actual parameter value matching the supplied formal parameter.
        /// </summary>
        /// <param name="formalParameter">The formal parameter.</param>
        /// <param name="parameterMap">
        /// A map from actual parameter names to formal parameter names. Used when parameters have been resolved
        /// using type, to identify their mapped names.
        /// </param>
        /// <param name="relaxed">When true, will match by type when name matching fails.</param>
        /// <returns>The actual parameter value matching the formal parameter or null if an error occurs.</returns>
        public object GetParameterValue(ParameterInfo formalParameter, Dictionary<string, string> parameterMap, bool relaxed)
        {
            if (SupportedSpecializedParameter(formalParameter))
            {
                return this.GetSpecializedParameter(formalParameter);
            }
            
            var actualParameterName = GetActualParameterName(formalParameter, parameterMap, relaxed);
            if (string.IsNullOrEmpty(actualParameterName))
            {
                return null;
            }
            
            if (ActualParameters.TryGetValue(actualParameterName, out var parameterValue))
            {
                if (formalParameter.ParameterType == typeof(string))
                {
                    return parameterValue.ToString();
                }
                else if (formalParameter.ParameterType.IsEnum)
                {

                    try
                    {
                        return Enum.Parse(formalParameter.ParameterType, ConduitUtilities.SanitizeString(parameterValue.ToString()), true);
                    }
                    catch (Exception e)
                    {
                        VLog.E($"Parameter Provider - Parameter '{parameterValue}' could not be cast to enum\nEnum Type: {formalParameter.ParameterType.FullName}\n{e}");
                        throw;
                    }
                }
                else
                {
                    try
                    {
                        return Convert.ChangeType(parameterValue, formalParameter.ParameterType);
                    }
                    catch (Exception e)
                    {
                        VLog.E($"Parameter Provider - Parameter '{parameterValue}' could not be cast\nType: {formalParameter.ParameterType.FullName}\n{e}");
                        return null;
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of parameter names that hold values of the specified type.
        /// Note: This is an expensive operation.
        /// </summary>
        /// <param name="targetType">The type we are querying.</param>
        /// <returns>The names of the parameters that match this type.</returns>
        public List<string> GetParameterNamesOfType(Type targetType)
        {
            if (_parametersOfType.ContainsKey(targetType))
            {
                return _parametersOfType[targetType];
            }
            
            var parameters = new List<string>();

            foreach (var parameter in ActualParameters)
            {
                if (parameter.Value.GetType() == targetType)
                {
                    parameters.Add(parameter.Key);
                }
            }

            return _parametersOfType[targetType] = parameters;
        }

        /// <summary>
        /// Returns true if the specified parameter can be resolved. GetSpecializedParameter must be able to return
        /// a valid value if this method returns true.
        /// </summary>
        /// <param name="formalParameter">The formal parameter.</param>
        /// <returns>True if this parameter can be resolved. False otherwise.</returns>
        protected virtual bool SupportedSpecializedParameter(ParameterInfo formalParameter)
        {
            return _specializedParameters.ContainsKey(formalParameter.ParameterType);
        }

        /// <summary>
        /// Returns the value of the specified parameter.
        /// </summary>
        /// <param name="formalParameter">The formal parameter.</param>
        /// <returns>The actual (supplied) invocation value for the parameter.</returns>
        protected virtual object GetSpecializedParameter(ParameterInfo formalParameter)
        {
            if (_specializedParameters.ContainsKey(formalParameter.ParameterType))
            {
                var parameterName = _specializedParameters[formalParameter.ParameterType];
                if (ActualParameters.ContainsKey(parameterName))
                {
                    return ActualParameters[parameterName];
                }
            }
            
            // Log warning when not found
            var error = new StringBuilder();
            error.AppendLine("Specialized parameter not found");
            error.AppendLine($"Parameter Type: {formalParameter.ParameterType}");
            error.AppendLine($"Parameter Name: {formalParameter.Name}");
            error.AppendLine($"Actual Parameters: {ActualParameters.Keys.Count}");
            foreach (var key in ActualParameters.Keys)
            {
                var val = ActualParameters[key] == null ? "NULL" : ActualParameters[key].GetType().ToString();
                error.AppendLine($"\t{key}: {val}");
            }
            VLog.W(error.ToString());
            return null;
        }
        
        /// <summary>
        /// Returns a list of all types that fit the parameter. 
        /// </summary>
        /// <param name="typeString">The textual expression of the type.</param>
        /// <param name="value">The value obtained. Used to validate some types.</param>
        /// <returns>
        /// The possible parameter types. Types that would result in data loss are omitted.
        /// </returns>
        private IEnumerable<Type> GetParameterTypes(string typeString, string value)
        {
            if (_customTypes.ContainsKey(typeString))
            {
                return new List<Type>() { _customTypes[typeString] };
            }

            if (!BuiltInTypes.ContainsKey(typeString) || BuiltInTypes[typeString].Count == 0)
            {
                return new List<Type>() { typeof(string) };
            }

            return BuiltInTypes[typeString].Where(type => PerfectTypeMatch(type, value)).ToList();
        }

        private bool PerfectTypeMatch(Type targetType, string value)
        {
            try
            {
                var valueAsTarget = Convert.ChangeType(value, targetType);

                if (valueAsTarget == null)
                {
                    return false;
                }
                
                if (!targetType.IsPrimitive)
                {
                    return true;
                }

                return value.Equals(valueAsTarget.ToString());
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts the parameter name that the formal parameter should be matched with. This may attempt to match the name by type
        /// if there is no exact match and the relaxed flag is set to true.
        /// </summary>
        /// <param name="formalParameter">The parameter info we are trying to find a value for.</param>
        /// <param name="parameterMap"></param>
        /// <param name="relaxed">When true, will allow matching by type when exact matching fails.</param>
        /// <returns>The matched actual parameter name if found, or null otherwise.</returns>
        private string GetActualParameterName(ParameterInfo formalParameter, Dictionary<string, string> parameterMap,
            bool relaxed)
        {
            var formalParameterName = formalParameter.Name;
            string targetActualParameterName;

            if (parameterMap.ContainsKey(formalParameterName))
            {
                targetActualParameterName = parameterMap[formalParameterName];
            }
            else
            {
                targetActualParameterName = formalParameterName;
            }
            
            if (ActualParameters.ContainsKey(targetActualParameterName))
            {
                return targetActualParameterName;
            }

            if (_parameterToRoleMap.ContainsKey(targetActualParameterName))
            {
                var roleName = _parameterToRoleMap[targetActualParameterName];
                if (!string.IsNullOrEmpty(roleName) && this.ActualParameters.ContainsKey(roleName))
                {
                    return roleName;
                }
            }

            if (!relaxed)
            {
                VLog.E($"Parameter '{formalParameterName}' is missing");
                return null;
            }

            var possibleNames = GetParameterNamesOfType(formalParameter.ParameterType);
            if (possibleNames.Count != 1)
            {
                VLog.E(
                    $"Got multiple parameters of type {formalParameter.ParameterType} but none with the correct name");
                return null;
            }

            targetActualParameterName = possibleNames[0];

            return targetActualParameterName;
        }

        public override string ToString()
        {
            return string.Join("',", AllParameterNames);
        }
    }

}
