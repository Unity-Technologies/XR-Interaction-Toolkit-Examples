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
    public class WitConfigurationEntitiesTab: WitConfigurationEditorTab
    {
        public override int TabOrder { get; } = 2;
        public override string TabID { get; } = "entities";
        public override string TabLabel { get; } = WitTexts.Texts.ConfigurationEntitiesTabLabel;
        public override string MissingLabel { get; } = WitTexts.Texts.ConfigurationEntitiesMissingLabel;
        public override bool ShouldTabShow(WitAppInfo appInfo)
        {
            return null != appInfo.entities;
        }
    }

}
