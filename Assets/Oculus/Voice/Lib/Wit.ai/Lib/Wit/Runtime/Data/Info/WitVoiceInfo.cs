/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitVoiceInfo
    {
        public string name;
        public string locale;
        public string gender;
        public string[] styles;
    }
}
