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
    public class WitConfigurationTraitsTab : WitConfigurationEditorTab
    {

        public override int TabOrder { get; } = 3;
        public override string TabID { get; } = "traits";
        public override string TabLabel { get; } = WitTexts.Texts.ConfigurationTraitsTabLabel;
        public override string MissingLabel { get; } = WitTexts.Texts.ConfigurationTraitsMissingLabel;
        public override bool ShouldTabShow(WitAppInfo appInfo)
        {
            return null != appInfo.traits;
        }
    }
}
