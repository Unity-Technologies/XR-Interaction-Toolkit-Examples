/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit
{
    /// <summary>
    /// This can optionally be used on enum values to provide additional information.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class ConduitValueAttribute : Attribute
    {
        public ConduitValueAttribute(params string[] aliases)
        {
            this.Aliases = aliases;
        }

        /// <summary>
        /// Different ways to refer to the same value. The first alias in the list will be treated as
        /// keyword and additional aliases as synonyms.
        /// Note: that if an alias is supplied, the original enum name is
        /// not considered as an alias or keyword anymore unless explicitly specified as an alias.
        /// </summary>
        public string[] Aliases { get; }
    }
}
