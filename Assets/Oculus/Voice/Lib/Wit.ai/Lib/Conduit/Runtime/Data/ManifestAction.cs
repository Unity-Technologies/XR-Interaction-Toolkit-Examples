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
    /// An action entry in the manifest.
    /// </summary>
    internal class ManifestAction: IManifestMethod
    {
        /// <summary>
        /// Called via JSON reflection, need preserver or it will be stripped on compile
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public ManifestAction() { }

        /// <summary>
        /// This is the internal fully qualified name of the method in the codebase.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The fully qualified name of the assembly containing the code for the action.
        /// </summary>
        public string Assembly { get; set; }

        /// <summary>
        /// The name of the action as exposed to the backend.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The parameters used by the action.
        /// </summary>
        public List<ManifestParameter> Parameters { get; set; } = new List<ManifestParameter>();

        /// <summary>
        /// Returns the fully qualified name of the declaring type of the action.
        /// </summary>
        [JsonIgnore]
        public string DeclaringTypeName => ID.Substring(0, ID.LastIndexOf('.'));

        /// <summary>
        /// Additional names by which the backend can refer to this action.
        /// </summary>
        public List<string> Aliases { get; set; } = new List<string>();

        public override bool Equals(object obj)
        {
            return obj is ManifestAction other && this.Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + ID.GetHashCode();
            hash = hash * 31 + Assembly.GetHashCode();
            hash = hash * 31 + Name.GetHashCode();
            hash = hash * 31 + Parameters.GetHashCode();
            hash = hash * 31 + Aliases.GetHashCode();
            return hash;
        }

        private bool Equals(ManifestAction other)
        {
            return this.ID == other.ID && this.Assembly == other.Assembly && this.Name == other.Name && this.Parameters.SequenceEqual(other.Parameters) && this.Aliases.SequenceEqual(other.Aliases);
        }
    }
}
