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

namespace Meta.Conduit
{
    /// <summary>
    /// Holds the details required to invoke a method at runtime.
    /// </summary>
    internal class InvocationContext
    {
        /// <summary>
        /// The type that declares the method.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The method information.
        /// </summary>
        public MethodInfo MethodInfo { get; set; }

        /// <summary>
        /// The minimum confidence necessary to invoke this method.
        /// </summary>
        public float MinConfidence { get; set; } = 0;

        /// <summary>
        /// The maximum confidence allowed to invoke this method.
        /// </summary>
        public float MaxConfidence { get; set; } = 1;

        /// <summary>
        /// Whether partial responses should be validated
        /// </summary>
        public bool ValidatePartial { get; set; } = false;

        /// <summary>
        /// If the invocation context is resolved, this will map formal parameter names to  actual (incoming) parameter
        /// names if they are different. This may be empty if no mapping is required or the context is not resolved.
        /// The key is the formal parameter name and value is the actual parameter name. 
        /// </summary>
        public Dictionary<string, string> ParameterMap { get; set; } = new Dictionary<string, string>();

        
        /// <summary>
        /// Saving the attribute type for the current context.
        /// </summary>
        public Type CustomAttributeType { get; set; } 
    }
}
