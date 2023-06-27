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
    internal interface IWitInfoVRequest : IWitVRequest
    {
        bool RequestAppId(VRequest.RequestCompleteDelegate<string> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestApps(int limit, int offset, VRequest.RequestCompleteDelegate<WitAppInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestAppInfo(string applicationId, VRequest.RequestCompleteDelegate<WitAppInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestClientAppToken(string applicationId, VRequest.RequestCompleteDelegate<string> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestIntentList(VRequest.RequestCompleteDelegate<WitIntentInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestIntentInfo(string intentId, VRequest.RequestCompleteDelegate<WitIntentInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestEntityList(VRequest.RequestCompleteDelegate<WitEntityInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestEntityInfo(string entityId, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestTraitList(VRequest.RequestCompleteDelegate<WitTraitInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestTraitInfo(string traitId, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestVoiceList(VRequest.RequestCompleteDelegate<Dictionary<string, WitVoiceInfo[]>> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);
    }
}
