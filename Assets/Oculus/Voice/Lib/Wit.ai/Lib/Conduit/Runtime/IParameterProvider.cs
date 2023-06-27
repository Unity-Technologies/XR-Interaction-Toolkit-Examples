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
using System.Text;
using Meta.WitAi.Json;

namespace Meta.Conduit
{
    /// <summary>
    /// Resolves parameters for invoking callbacks.
    /// </summary>
    internal interface IParameterProvider
    {
        /// <summary>
        /// The list of the names of all parameters in the provider. 
        /// </summary>
        List<string> AllParameterNames { get; }
        
        /// <summary>
        /// Populates the parameters from a Wit.Ai response node.
        /// Must be called after all parameters have been obtained from Wit.Ai and mapped but before any are read.
        /// </summary>
        void PopulateParametersFromNode(WitResponseNode responseNode);
        
        /// <summary>
        /// Populates the roles mappings between actual parameters and their roles..
        /// Must be called after all parameters have been populated using PopulateParameters but before any are read.
        /// </summary>
        /// <param name="parameterToRoleMap">
        /// Keys are normalized lowercase internal (code) names.
        /// Values are fully qualified parameter names (roles)
        /// </param>
        void PopulateRoles(Dictionary<string, string> parameterToRoleMap);

        /// <summary>
        /// Explicitly adds a parameter to the provider.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        void AddParameter(string parameterName, object value);

        /// <summary>
        /// Returns true if a parameter with the specified name can be provided.
        /// </summary>
        /// <param name="parameter">The name of the parameter.</param>
        /// <param name="log">The log string builder.</param>
        /// <returns>True if a parameter with the specified name can be provided.</returns>
        bool ContainsParameter(ParameterInfo parameter, StringBuilder log);

        /// <summary>
        /// Add a custom known type (typically enum) to the provider.
        /// This should be called BEFORE calling any of the population methods.
        /// </summary>
        /// <param name="name">The internal name of the type.</param>
        /// <param name="type">The data type.</param>
        void AddCustomType(string name, Type type);

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
        object GetParameterValue(ParameterInfo formalParameter, Dictionary<string, string> parameterMap,
            bool relaxed);

        /// <summary>
        /// Returns a list of parameter names that hold values of the specified type.
        /// Note: This is an expensive operation.
        /// </summary>
        /// <param name="targetType">The type we are querying.</param>
        /// <returns>The names of the parameters that match this type.</returns>
        List<string> GetParameterNamesOfType(Type targetType);

        /// <summary>
        /// Registers a certain keyword as reserved for a specialized parameter.
        /// </summary>
        /// <param name="reservedParameterName">The name of the specialized parameter. For example @WitResponseNode</param>
        /// <param name="parameterType">The data type of the parameter</param>
        void SetSpecializedParameter(string reservedParameterName, Type parameterType);
    }
}
