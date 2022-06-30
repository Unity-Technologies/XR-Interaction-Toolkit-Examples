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
    public class WitSimpleDynamicEntity : IDynamicEntitiesProvider
    {
        public List<string> keywords;
        public string entity;

        public WitSimpleDynamicEntity(string entityIdentifier, List<string> words)
        {
            entity = entityIdentifier;
            keywords = words;
        }

        public KeyValuePair<string, WitResponseArray> GetEntityPair() {
            var keywordEntries = new WitResponseArray();
            foreach (string keyword in keywords)
            {
                var synonyms = new WitResponseArray();
                synonyms.Add(new WitResponseData(keyword));

                var keywordEntry = new WitResponseClass();
                keywordEntry.Add("keyword", new WitResponseData(keyword));
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
