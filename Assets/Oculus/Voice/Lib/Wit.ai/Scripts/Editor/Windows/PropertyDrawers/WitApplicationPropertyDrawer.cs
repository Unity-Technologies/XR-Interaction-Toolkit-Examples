/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using System.Reflection;

namespace Meta.WitAi.Windows
{
    public class WitApplicationPropertyDrawer : WitPropertyDrawer
    {
        // Whether to use a foldout
        protected override bool FoldoutEnabled => false;
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Determine by ids
            switch (key)
            {
                case LocalizedTitleKey:
                    return WitTexts.Texts.ConfigurationApplicationTabLabel;
                case LocalizedMissingKey:
                    return WitTexts.Texts.ConfigurationApplicationMissingLabel;
                case "name":
                    return WitTexts.Texts.ConfigurationApplicationNameLabel;
                case "id":
                    return WitTexts.Texts.ConfigurationApplicationIdLabel;
                case "lang":
                    return WitTexts.Texts.ConfigurationApplicationLanguageLabel;
                case "isPrivate":
                    return WitTexts.Texts.ConfigurationApplicationPrivateLabel;
                case "createdAt":
                    return WitTexts.Texts.ConfigurationApplicationCreatedLabel;
                case "trainingStatus":
                    return WitTexts.Texts.ConfigurationApplicationTrainingStatus;
                case "lastTrainDuration":
                    return WitTexts.Texts.ConfigurationApplicationTrainingLastDuration;
                case "lastTrainedAt":
                    return WitTexts.Texts.ConfigurationApplicationTrainingLast;
                case "nextTrainAt":
                    return WitTexts.Texts.ConfigurationApplicationTrainingNext;
            }

            // Default to base
            return base.GetLocalizedText(property, key);
        }
        // Skip wit configuration field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "intents":
                case "entities":
                case "traits":
                case "voices":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }
    }
}
