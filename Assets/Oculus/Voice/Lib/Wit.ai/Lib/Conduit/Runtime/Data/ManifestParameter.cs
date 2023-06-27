/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using Meta.WitAi.Json;

namespace Meta.Conduit
{
    /// <summary>
    /// Represents a method parameter/argument in the manifest.
    /// </summary>
    internal class ManifestParameter
    {
        /// <summary>
        /// Called via JSON reflection, need preserver or it will be stripped on compile
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public ManifestParameter() { }

        /// <summary>
        /// This is the parameter name as exposed to the backend (slot or role)
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = ConduitUtilities.DelimitWithUnderscores(value);
        }
        private string _name;

        /// <summary>
        /// This is the technical name of the parameter in the actual method in codebase.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// A fully qualified name exposed to the backend for uniqueness.
        /// </summary>
        public string QualifiedName { get; set; }

        /// <summary>
        /// This is the data type of the parameter, exposed as an entity type.
        /// </summary>
        public string EntityType
        {
            get
            {
                var lastPeriod = QualifiedTypeName.LastIndexOf('.');
                if (lastPeriod < 0)
                {
                    return QualifiedTypeName;
                }
                var entityName = QualifiedTypeName.Substring(lastPeriod + 1);

                // Identify whether it's a nested type
                var lastPlus = entityName.LastIndexOf('+');

                if (lastPlus < 0)
                {
                    return entityName;
                }

                return entityName.Substring(lastPlus + 1);
            }
        }

        /// <summary>
        /// The assembly containing the data type.
        /// </summary>
        public string TypeAssembly { get; set; }

        /// <summary>
        /// The fully qualified name of the parameter data type.
        /// </summary>
        public string QualifiedTypeName { get; set; }

        /// <summary>
        /// Additional names by which the backend can refer to this parameter.
        /// </summary>
        public List<string> Aliases { get; set; }
        
        /// <summary>
        /// Example values this parameter can accept.
        /// </summary>
        public List<string> Examples { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ManifestParameter other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + _name.GetHashCode();
            hash = hash * 31 + InternalName.GetHashCode();
            hash = hash * 31 + QualifiedName.GetHashCode();
            hash = hash * 31 + TypeAssembly.GetHashCode();
            hash = hash * 31 + QualifiedTypeName.GetHashCode();
            hash = hash * 31 + Aliases.GetHashCode();
            return hash;
        }

        private bool Equals(ManifestParameter other)
        {
            return Equals(this.InternalName, other.InternalName) && Equals(this.QualifiedName, other.QualifiedName) &&
                   Equals(this.EntityType, other.EntityType) && this.Aliases.SequenceEqual(other.Aliases) &&
                   Equals(this.TypeAssembly, other.TypeAssembly) &&
                   Equals(this.QualifiedTypeName, other.QualifiedTypeName);
        }
    }
}
