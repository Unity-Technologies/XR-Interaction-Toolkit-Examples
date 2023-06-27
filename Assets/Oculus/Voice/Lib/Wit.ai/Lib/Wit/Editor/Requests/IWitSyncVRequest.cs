/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Data.Info;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    internal interface IWitSyncVRequest : IWitVRequest
    {
        /// <summary>
        /// Submits an intent to be added to the current wit app
        /// </summary>
        /// <param name="intentInfo">The intent data to be submitted</param>
        /// <param name="onComplete">On completion that returns an intent with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddIntent(WitIntentInfo intentInfo, VRequest.RequestCompleteDelegate<WitIntentInfo> onComplete);

        /// <summary>
        /// Submits an entity to be added to the current wit app
        /// </summary>
        /// <param name="entityInfo">The entity info to be submitted</param>
        /// <param name="onComplete">On completion that returns an entity with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddEntity(WitEntityInfo entityInfo, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a keyword to be added to an entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity this keyword should be added to</param>
        /// <param name="keywordInfo">The keyword & synonyms submitted</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddEntityKeyword(string entityId,
            WitEntityKeywordInfo keywordInfo, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a synonym to be added to a keyword on the specified entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity that holds the keyword</param>
        /// <param name="keyword">The keyword we're adding the synonym to</param>
        /// <param name="synonym">The synonym we're adding</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddSynonym(string entityId, string keyword, string synonym,
            VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a trait to be added to the current wit app
        /// </summary>
        /// <param name="traitInfo">The trait data to be submitted</param>
        /// <param name="onComplete">On completion that returns a trait with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddTrait(WitTraitInfo traitInfo, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete);

        /// <summary>
        /// Submits a trait value to be added to the current wit app
        /// </summary>
        /// <param name="traitId">The trait id to be submitted</param>
        /// <param name="traitValue">The trait value to be submitted</param>
        /// <param name="onComplete">On completion callback that returns updated trait if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddTraitValue(string traitId,
            string traitValue, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete);
    }
}
