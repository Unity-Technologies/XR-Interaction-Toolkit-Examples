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
    public class WitConfigurationApplicationTab : WitConfigurationEditorTab
    {
        public override string TabID { get; } = "application";
        public override int TabOrder { get; } = 0;
        public override string TabLabel { get; } = WitTexts.Texts.ConfigurationApplicationTabLabel;
        public override string MissingLabel { get; } = WitTexts.Texts.ConfigurationApplicationMissingLabel;
        public override bool ShouldTabShow(WitAppInfo appInfo)
        {
            return true;
        }
        public override string GetPropertyName(string tabID)
        {
            return "_appInfo";
        }
    }
}
