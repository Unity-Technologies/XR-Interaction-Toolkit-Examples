/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Data;
using Meta.WitAi.Json;
using Meta.Conduit.Editor;

namespace Meta.WitAi.Windows
{
    /// <summary>
    /// Validates whether a data type if supported by Wit.
    /// </summary>
    internal class WitParameterValidator : IParameterValidator
    {
        /// <summary>
        /// These are the types that we natively support.
        /// </summary>
        private readonly HashSet<Type> _builtInTypes = new HashSet<Type>()
            { typeof(string), typeof(int), typeof(DateTime), typeof(float), typeof(double), typeof(decimal) };

        /// <summary>
        /// Tests if a parameter type can be supplied directly to a callback method from.
        /// </summary>
        /// <param name="type">The data type.</param>
        /// <returns>True if the parameter type is supported. False otherwise.</returns>
        public bool IsSupportedParameterType(Type type)
        {
            return type.IsEnum || _builtInTypes.Contains(type) || type == typeof(WitResponseNode) || type == typeof(VoiceSession) || type == typeof(Exception);
        }
    }
}
