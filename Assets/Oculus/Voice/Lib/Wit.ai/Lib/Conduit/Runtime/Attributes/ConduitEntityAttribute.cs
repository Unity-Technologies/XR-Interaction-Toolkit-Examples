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
    /// Marks an enumeration as a Conduit entity to be included in the generated manifest. Enums that are directly used
    /// in a parameter of Conduit methods do not need this attribute.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Enum)]
    public class ConduitEntityAttribute : Attribute
    {
        /// <summary>
        /// The current name of this entity
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The specific entity id
        /// </summary>
        public string ID { get; }

        // ID used for auto generation
        public ConduitEntityAttribute(string name, string id = null)
        {
            this.Name = name;
            this.ID = id;
        }
    }
}
