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
using Meta.WitAi.Lib;
using Meta.Conduit.Editor;
using Meta.WitAi.Json;

namespace Meta.WitAi.Windows
{
    /// <summary>
    /// Filters out parameters of specific types.
    /// </summary>
    internal class WitParameterFilter : IParameterFilter
    {
        /// <summary>
        /// Tests if a parameter type should be filtered out.
        /// </summary>
        /// <param name="type">The data type.</param>
        /// <returns>True if the parameter type should be filtered out. False otherwise.</returns>
        public bool ShouldFilterOut(Type type)
        {
            return type == typeof(WitResponseNode) || type == typeof(VoiceSession);
        }

        public bool ShouldFilterOut(string typeName)
        {
            return typeName == nameof(WitResponseNode) || typeName == nameof(VoiceSession);
        }
    }
}
