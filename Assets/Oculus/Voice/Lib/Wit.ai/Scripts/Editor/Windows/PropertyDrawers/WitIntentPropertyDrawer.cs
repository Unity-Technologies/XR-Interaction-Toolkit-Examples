/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows
{
    public class WitIntentPropertyDrawer : WitPropertyDrawer
    {
        // Maps the expansion status of references foldouts
        private readonly Dictionary<string, bool> _referencesExpanded = new Dictionary<string, bool>();
        
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
                    return WitTexts.Texts.ConfigurationIntentsIdLabel;
                case "entities":
                    return WitTexts.Texts.ConfigurationIntentsEntitiesLabel;
            }

            // Default to base
            return base.GetLocalizedText(property, key);
        }

        // Layout entity override
        protected override void LayoutPropertyField(FieldInfo subfield, SerializedProperty subfieldProperty, GUIContent labelContent, bool canEdit)
        {
            // Handle all the same except entities
            if (canEdit || !string.Equals(subfield.Name, "entities"))
            {
                base.LayoutPropertyField(subfield, subfieldProperty, labelContent, canEdit);
                return;
            }

            // Entity foldout
            subfieldProperty.isExpanded = WitEditorUI.LayoutFoldout(labelContent, subfieldProperty.isExpanded);
            if (!subfieldProperty.isExpanded)
            {
                return;
            }
            
            EditorGUI.indentLevel++;
            if (subfieldProperty.arraySize == 0)
            {
                WitEditorUI.LayoutErrorLabel(WitTexts.Texts.ConfigurationEntitiesMissingLabel);
            }
            else
            {
                for (int i = 0; i < subfieldProperty.arraySize; i++)
                {
                    SerializedProperty entityProp = subfieldProperty.GetArrayElementAtIndex(i);
                    string entityPropName = entityProp.FindPropertyRelative("name").stringValue;
                    WitEditorUI.LayoutLabel(entityPropName);
                }
            }
            EditorGUI.indentLevel--;
        }

        protected override void OnGUIPostFields(Rect position, SerializedProperty property, GUIContent label)
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

            var manifest = ManifestLoader.LoadManifest(configuration.ManifestLocalPath);

            var intentName = property.FindPropertyRelative("name")?.stringValue;

            if (!manifest.ContainsAction(intentName))
            {
                return;
            }

            var contexts = manifest.GetInvocationContexts(intentName);
            
            if (!_referencesExpanded.ContainsKey(intentName))
            {
                _referencesExpanded[intentName] = false;
            }
            _referencesExpanded[intentName] = WitEditorUI.LayoutFoldout(new GUIContent("References"), _referencesExpanded[intentName]);
            if (!_referencesExpanded[intentName])
            {
                return;
            }
            
            EditorGUI.indentLevel++;
            foreach (var context in contexts)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(EditorGUI.indentLevel * WitStyles.IndentationSpaces);
                    if (WitEditorUI.LayoutTextLink($"{context.Type.Name}::{context.MethodInfo.Name}()"))
                    {
                        assemblyWalker.GetSourceCode(context.Type, out var sourceCodeFile, out _);
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(sourceCodeFile, 1);
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
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
    }
}
