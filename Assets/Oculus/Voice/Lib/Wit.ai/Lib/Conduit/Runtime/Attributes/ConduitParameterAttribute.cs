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

namespace Meta.Conduit
{
    /// <summary>
    /// Marks a parameter as a Conduit parameter to be supplied when the callback method is called.
    /// This is not required, but allows the addition of more information about parameters to improve the quality of
    /// intent recognition and entity resolution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ConduitParameterAttribute : Attribute
    {
        /// <summary>
        /// Example values for the attribute. This may increase the accuracy of recognition. 
        /// </summary>
        public List<string> Examples { get; }
        
        /// <summary>
        /// Additional names that can refer to this parameter.
        /// </summary>
        public List<string> Aliases { get; }

        /// <summary>
        /// Initializes the attribute with examples.
        /// </summary>
        /// <param name="examples">
        /// Examples values that can be supplied for this parameter.
        /// Used to increase accuracy of generated utterances. Highly recommended for string parameters.
        /// </param>
        public ConduitParameterAttribute(params string[] examples)
        {
            this.Examples = examples.ToList();
            this.Aliases = new List<string>();
        }

        /// <summary>
        /// Initializes the attribute with aliases and examples.
        /// </summary>
        /// <param name="aliases">
        /// Different names to refer to this parameter.
        /// </param>
        /// <param name="examples">
        /// Examples values that can be supplied for this parameter.
        /// Used to increase accuracy of generated utterances. Highly recommended for string parameters.
        /// </param>
        public ConduitParameterAttribute(string[] aliases, params string[] examples)
        {
            this.Examples = examples.ToList();
            this.Aliases = aliases.ToList();
        }
    }
}
