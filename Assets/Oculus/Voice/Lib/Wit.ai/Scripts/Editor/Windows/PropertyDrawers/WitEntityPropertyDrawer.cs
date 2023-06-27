/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using System.Reflection;
using Meta.WitAi.Data.Configuration;
using UnityEngine;

namespace Meta.WitAi.Windows
{
    public class WitEntityPropertyDrawer : WitPropertyDrawer
    {
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Determine by ids
            switch (key)
            {
                    case LocalizedTitleKey:
                        string title = GetFieldStringValue(property, "name");
                        if (!string.IsNullOrEmpty(title))
                        {
                            return title;
                        }
                        break;
                    case "id":
                        return WitTexts.Texts.ConfigurationEntitiesIdLabel;
                    case "lookups":
                        return WitTexts.Texts.ConfigurationEntitiesLookupsLabel;
                    case "roles":
                        return WitTexts.Texts.ConfigurationEntitiesRolesLabel;
                    case "keywords":
                        return WitTexts.Texts.ConfigurationEntitiesKeywordsLabel;
            }

            // Default to base
            return base.GetLocalizedText(property, key);
        }
        // Determine if should layout field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "name":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }

        protected override void OnDrawLabelInline(SerializedProperty property)
        {
            var configuration = property.serializedObject.targetObject as WitConfiguration;
            if (configuration == null || !configuration.useConduit)
            {
                return;
            }
            
            var assemblyWalker = WitConfigurationEditor.AssemblyWalker;
            if (assemblyWalker == null)
            {
                return;
            }

            var entityName = property.displayName;

            if (WitEditorUI.LayoutIconButton(EditorGUIUtility.IconContent("UxmlScript Icon")))
            {
                var manifest = ManifestLoader.LoadManifest(configuration.ManifestLocalPath);
                var sourceCodeFile = CodeMapper.GetSourceFilePathFromTypeName(entityName, manifest, assemblyWalker);

                if (string.IsNullOrEmpty(sourceCodeFile))
                {
                    VLog.W($"Failed to local source code for {entityName}");
                    return;
                }
                
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sourceCodeFile, 1);
            }
        }
    }
}
