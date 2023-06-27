/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi.Data.Entities
{
    [Serializable]
    public class WitDynamicEntity : IDynamicEntitiesProvider
    {
        public string entity;
        public List<WitEntityKeywordInfo> keywords = new List<WitEntityKeywordInfo>();

        public WitDynamicEntity()
        {
        }

        public WitDynamicEntity(string entity, WitEntityKeywordInfo keyword)
        {
            this.entity = entity;
            this.keywords.Add(keyword);
        }

        public WitDynamicEntity(string entity, params string[] keywords)
        {
            this.entity = entity;
            foreach (var keyword in keywords)
            {
                this.keywords.Add(new WitEntityKeywordInfo()
                {
                    keyword = keyword,
                    synonyms = new List<string>(new string[] { keyword })
                });
            }
        }

        public WitDynamicEntity(string entity, Dictionary<string, List<string>> keywordsToSynonyms)
        {
            this.entity = entity;

            foreach (var synonym in keywordsToSynonyms)
            {
                keywords.Add(new WitEntityKeywordInfo()
                {
                    keyword = synonym.Key,
                    synonyms = synonym.Value
                });

            }
        }

        public WitResponseArray AsJson
        {
            get
            {
                return JsonConvert.SerializeToken(keywords).AsArray;
            }
        }

        public WitDynamicEntities GetDynamicEntities()
        {
            return new WitDynamicEntities()
            {
                entities = new List<WitDynamicEntity>
                {
                    this
                }
            };
        }
    }
}
