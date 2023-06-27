/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitEntityInfo
    {
        /// <summary>
        /// Entity display name
        /// </summary>
        [SerializeField] public string name;
        /// <summary>
        /// Entity unique identifier
        /// </summary>
        [SerializeField] public string id;
        /// <summary>
        /// Various lookup options for this entity
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        [SerializeField] public string[] lookups;
        /// <summary>
        /// Various roles in which this
        /// entity may be used
        /// </summary>
        [SerializeField] public WitEntityRoleInfo[] roles;
        /// <summary>
        /// Mapped keywords and their analyzed
        /// synonyms
        /// </summary>
        [SerializeField] public WitEntityKeywordInfo[] keywords;

        public override bool Equals(object obj)
        {
            if (obj is WitEntityInfo other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(WitEntityInfo other)
        {
            return name == other.name && id == other.id && lookups.Equivalent(other.lookups) && roles.Equivalent(other.roles) && keywords.Equivalent(other.keywords);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + name.GetHashCode();
            hash = hash * 31 + id.GetHashCode();
            hash = hash * 31 + lookups.GetHashCode();
            hash = hash * 31 + roles.GetHashCode();
            hash = hash * 31 + keywords.GetHashCode();
            return hash;
        }
    }
}
