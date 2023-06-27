/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitEntityKeywordInfo
    {
        public WitEntityKeywordInfo(string keyword, List<string> synonyms = null)
        {
            this.keyword = keyword;
            this.synonyms = synonyms??new List<string>();
        }

        /// <summary>
        /// Unique keyword identifier
        /// </summary>
        public string keyword;
        /// <summary>
        /// Synonyms for specified keyword
        /// </summary>
        #if UNITY_2021_1_OR_NEWER
        [UnityEngine.NonReorderable]
        #endif
        public List<string> synonyms;

        public override bool Equals(object obj)
        {
            if (obj is WitEntityKeywordInfo other)
            {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(WitEntityKeywordInfo other)
        {
            return keyword == other.keyword && synonyms.Equivalent(other.synonyms);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + keyword.GetHashCode();
            hash = hash * 31 + synonyms.GetHashCode();
            return hash;
        }
    }
}
