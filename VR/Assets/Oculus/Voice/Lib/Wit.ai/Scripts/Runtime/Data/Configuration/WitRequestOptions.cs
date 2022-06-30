/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Interfaces;

namespace Facebook.WitAi.Configuration
{
    public class WitRequestOptions
    {
        public IDynamicEntitiesProvider dynamicEntities;
        public int nBestIntents = -1;
    }
}
