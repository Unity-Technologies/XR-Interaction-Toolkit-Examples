/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Keywords;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{

    [Serializable]
    public class WitEntity : WitConfigurationData
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public string[] lookups;
        [SerializeField] public string[] roles;
        [SerializeField] public WitKeyword[] keywords;

        #if UNITY_EDITOR
        protected override WitRequest OnCreateRequest()
        {
            return witConfiguration.GetEntityRequest(name);
        }

        public override void UpdateData(WitResponseNode entityWitResponse)
        {
            id = entityWitResponse["id"].Value;
            name = entityWitResponse["name"].Value;
            lookups = entityWitResponse["lookups"].AsStringArray;
            roles = entityWitResponse["roles"].AsStringArray;
            var keywordArray = entityWitResponse["keywords"].AsArray;
            keywords = new WitKeyword[keywordArray.Count];
            for (int i = 0; i < keywordArray.Count; i++)
            {
                keywords[i] = WitKeyword.FromJson(keywordArray[i]);
            }
        }

        public static WitEntity FromJson(WitResponseNode entityWitResponse)
        {
            var entity = new WitEntity();
            entity.UpdateData(entityWitResponse);
            return entity;
        }
        #endif
    }
}
