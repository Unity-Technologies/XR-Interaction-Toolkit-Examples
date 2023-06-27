/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using System.Collections.Generic;
using System.Web;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Data.Entities;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;

namespace Meta.WitAi
{
    public static class WitRequestFactory
    {
        private static VoiceServiceRequestOptions.QueryParam QueryParam(string key, string value)
        {
            return new VoiceServiceRequestOptions.QueryParam() { key = key, value = value };
        }

        private static void HandleWitRequestOptions(WitRequestOptions requestOptions,
            IDynamicEntitiesProvider[] additionalEntityProviders)
        {
            WitResponseClass entities = new WitResponseClass();
            bool hasEntities = false;

            if (null != additionalEntityProviders)
            {
                foreach (var provider in additionalEntityProviders)
                {
                    foreach (var providerEntity in provider.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, providerEntity);
                    }
                }
            }

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                foreach (var providerEntity in DynamicEntityKeywordRegistry.Instance.GetDynamicEntities())
                {
                    hasEntities = true;
                    MergeEntities(entities, providerEntity);
                }
            }

            if (null != requestOptions)
            {
                if (!string.IsNullOrEmpty(requestOptions.tag))
                {
                    requestOptions.QueryParams["tag"] = requestOptions.tag;
                }

                if (null != requestOptions.dynamicEntities)
                {
                    foreach (var entity in requestOptions.dynamicEntities.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, entity);
                    }
                }
            }

            if (hasEntities)
            {
                requestOptions.QueryParams["entities"] = entities.ToString();
            }
        }

        private static void MergeEntities(WitResponseClass entities, WitDynamicEntity providerEntity)
        {
            if (!entities.HasChild(providerEntity.entity))
            {
                entities[providerEntity.entity] = new WitResponseArray();
            }
            var mergedArray = entities[providerEntity.entity];
            Dictionary<string, WitResponseClass> map = new Dictionary<string, WitResponseClass>();
            HashSet<string> synonyms = new HashSet<string>();
            var existingKeywords = mergedArray.AsArray;
            for (int i = 0; i < existingKeywords.Count; i++)
            {
                var keyword = existingKeywords[i].AsObject;
                var key = keyword["keyword"].Value;
                if(!map.ContainsKey(key))
                {
                    map[key] = keyword;
                }
            }
            foreach (var keyword in providerEntity.keywords)
            {
                if (map.TryGetValue(keyword.keyword, out var keywordObject))
                {
                    foreach (var synonym in keyword.synonyms)
                    {
                        keywordObject["synonyms"].Add(synonym);
                    }
                }
                else
                {
                    keywordObject = JsonConvert.SerializeToken(keyword).AsObject;
                    map[keyword.keyword] = keywordObject;
                    mergedArray.Add(keywordObject);
                }
            }
        }

        private static WitRequestOptions GetSetupOptions(WitRequestOptions newOptions,
            IDynamicEntitiesProvider[] additionalDynamicEntities)
        {
            // Generate options exist
            WitRequestOptions options = newOptions != null ? newOptions : new WitRequestOptions();
            // Set intents
            if (-1 != options.nBestIntents)
            {
                options.QueryParams["n"] = options.nBestIntents.ToString();
            }
            // Set dynamic entities
            HandleWitRequestOptions(options, additionalDynamicEntities);
            // Set tag
            if (!string.IsNullOrEmpty(options.tag))
            {
                options.QueryParams["tag"] = options.tag;
            }
            return options;
        }

        /// <summary>
        /// Creates a message request that will process a query string with NLU
        /// </summary>
        /// <param name="config"></param>
        /// <param name="query">Text string to process with the NLU</param>
        /// <returns></returns>
        public static VoiceServiceRequest CreateMessageRequest(this WitConfiguration config, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents, IDynamicEntitiesProvider[] additionalEntityProviders = null)
        {
            var options = GetSetupOptions(requestOptions, additionalEntityProviders);
            return new WitUnityRequest(config, NLPRequestInputType.Text, options, requestEvents);
        }

        /// <summary>
        /// Creates a request for nlu processing that includes a data stream for mic data
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest CreateSpeechRequest(this WitConfiguration config, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents, IDynamicEntitiesProvider[] additionalEntityProviders = null)
        {
            var options = GetSetupOptions(requestOptions, additionalEntityProviders);
            var path = config.GetEndpointInfo().Speech;
            return new WitRequest(config, path, options, requestEvents);
        }

        /// <summary>
        /// Creates a request for getting the transcription from the mic data
        /// </summary>
        ///<param name="config"></param>
        /// <param name="requestOptions"></param>
        /// <returns>WitRequest</returns>
        public static WitRequest CreateDictationRequest(this WitConfiguration config, WitRequestOptions requestOptions, VoiceServiceRequestEvents requestEvents = null)
        {
            var options = GetSetupOptions(requestOptions, null);
            var path = config.GetEndpointInfo().Dictation;
            return new WitRequest(config, path, options, requestEvents);
        }
    }
}
