/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi;
using Meta.WitAi.Data.Info;
namespace Meta.WitAi.Data.Configuration.Tabs
{
    public class WitConfigurationIntentsTab : WitConfigurationEditorTab
    {
        public override int TabOrder { get; } = 1;
        public override string TabID { get; } = "intents";
        public override string TabLabel { get; } = WitTexts.Texts.ConfigurationIntentsTabLabel;
        public override string MissingLabel { get; } = WitTexts.Texts.ConfigurationIntentsMissingLabel;
        public override bool ShouldTabShow(WitAppInfo appInfo)
        {
            return null != appInfo.intents;
        }
    }
}
