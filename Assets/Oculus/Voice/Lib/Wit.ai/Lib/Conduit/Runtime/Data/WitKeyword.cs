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
using Meta.WitAi.Data.Info;

namespace Meta.Conduit
{
    public class WitKeyword
    {
        public readonly string keyword;

        public readonly HashSet<string> synonyms;

        public WitKeyword():this("", null)
        {
        }
        
        public WitKeyword(string keyword, List<string> synonyms = null)
        {
            this.keyword = keyword;
            this.synonyms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (synonyms == null)
            {
                return;
            }

            // Maintain list order when adding so we accept only first instance of repeated synonyms
            foreach (var synonym in synonyms)
            {
                if (!this.synonyms.Contains(synonym))
                {
                    this.synonyms.Add(synonym);
                }
            }
        }
        
        public WitKeyword(WitEntityKeywordInfo witEntityKeywordInfo): this(witEntityKeywordInfo.keyword, witEntityKeywordInfo.synonyms)
        {
        }

        public WitEntityKeywordInfo GetAsInfo() =>
            new WitEntityKeywordInfo()
            {
                keyword = this.keyword,
                synonyms = this.synonyms.ToList()
            };

        public override bool Equals(object obj)
        {
            if (obj is WitKeyword other)
            {
                return Equals(other);
            }

            return false;
        }

        private bool Equals(WitKeyword other)
        {
            return this.keyword.Equals(other.keyword) && this.synonyms.SequenceEqual(other.synonyms);
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
