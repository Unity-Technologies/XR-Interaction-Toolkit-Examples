/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi
{
    public static class WitConfigurationEditorUI
    {
        // Configuration select
        public static void LayoutConfigurationSelect(ref int configIndex, Action onNewClick)
        {
            // Refresh configurations if needed
            WitConfiguration[] witConfigs = WitConfigurationUtility.WitConfigs;

            // If no configuration exists, provide a means for the user to create a new one.
            if (witConfigs == null || witConfigs.Length == 0)
            {
                // Begin layout
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (WitEditorUI.LayoutTextButton(WitTexts.Texts.SettingsAddMainButtonLabel))
                {
                    onNewClick?.Invoke();
                }

                // End layout
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            // Clamp Config Index
            bool configUpdated = false;
            if (configIndex < 0 || configIndex >= witConfigs.Length)
            {
                configUpdated = true;
                configIndex = Mathf.Clamp(configIndex, 0, witConfigs.Length);
            }

            GUILayout.BeginHorizontal();

            // Layout popup
            WitEditorUI.LayoutPopup(WitTexts.Texts.ConfigurationSelectLabel, WitConfigurationUtility.WitConfigNames, ref configIndex, ref configUpdated);

            if (GUILayout.Button("", GUI.skin.GetStyle("IN ObjectField"), GUILayout.Width(15)))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(witConfigs[configIndex]);
            }

            GUILayout.EndHorizontal();
        }
    }
}
