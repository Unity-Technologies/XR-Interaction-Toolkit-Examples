/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;

namespace Facebook.WitAi.Data.Entities
{
    public class WitDynamicEntity : IDynamicEntitiesProvider
    {
        public string entity;
        public Dictionary<string, List<string>> keywordsToSynonyms;

        public WitDynamicEntity(string entity, Dictionary<string, List<string>> keywordsToSynonyms)
        {
            this.entity = entity;
            this.keywordsToSynonyms = keywordsToSynonyms;
        }

        public KeyValuePair<string, WitResponseArray> GetEntityPair() {
            var keywordEntries = new WitResponseArray();
            foreach (var keywordToSynonyms in keywordsToSynonyms)
            {
                var synonyms = new WitResponseArray();
                foreach (string synonym in keywordToSynonyms.Value)
                {
                    synonyms.Add(new WitResponseData(synonym));
                }

                var keywordEntry = new WitResponseClass();
                keywordEntry.Add("keyword", new WitResponseData(keywordToSynonyms.Key));
                keywordEntry.Add("synonyms", synonyms);

                keywordEntries.Add(keywordEntry);
            }
            return new KeyValuePair<string, WitResponseArray>(entity, keywordEntries);
        }

        public string ToJSON()
        {
            KeyValuePair<string, WitResponseArray> pair = this.GetEntityPair();
            var root = new WitResponseClass();
            root.Add(pair.Key, pair.Value);
            return root.ToString();
        }
    }
}
