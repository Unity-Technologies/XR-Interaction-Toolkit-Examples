/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Keywords
{
    [Serializable]
    public class WitKeyword
    {
        [SerializeField] public string keyword;
        [SerializeField] public string[] synonyms;

        #if UNITY_EDITOR
        public static WitKeyword FromJson(WitResponseNode keywordNode)
        {
            return new WitKeyword()
            {
                keyword = keywordNode["keyword"],
                synonyms = keywordNode["synonyms"].AsStringArray
            };
        }
        #endif
    }
}
