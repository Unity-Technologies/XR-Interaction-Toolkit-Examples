/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Json;

namespace Meta.WitAi.Data.Intents
{
    public class WitIntentData
    {
        public WitResponseNode responseNode;

        public string id;
        public string name;
        public float confidence;

        public WitIntentData() {}

        public WitIntentData(WitResponseNode node)
        {
            FromIntentWitResponseNode(node);
        }

        public WitIntentData FromIntentWitResponseNode(WitResponseNode node)
        {
            WitIntentData result = this;
            JsonConvert.DeserializeIntoObject(ref result, node);
            return result;
        }
    }
}
