/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi;
using Meta.WitAi.Requests;

namespace Lib.Wit.Runtime.Requests
{
    internal interface IWitVRequestFactory
    {
        IWitSyncVRequest CreateWitSyncVRequest(IWitRequestConfiguration configuration);

        IWitInfoVRequest CreateWitInfoVRequest(IWitRequestConfiguration configuration);
    }

    internal class WitVRequestFactory : IWitVRequestFactory
    {
        public IWitSyncVRequest CreateWitSyncVRequest(IWitRequestConfiguration configuration)
        {
            return new WitSyncVRequest(configuration);
        }

        public IWitInfoVRequest CreateWitInfoVRequest(IWitRequestConfiguration configuration)
        {
            return new WitInfoVRequest(configuration);
        }
    }
}
