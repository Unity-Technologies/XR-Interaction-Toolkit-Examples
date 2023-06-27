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
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows
{
    public class WitWindow : WitConfigurationWindow
    {
        protected WitConfigurationEditor witInspector;
        protected string serverToken;
        protected override GUIContent Title => WitTexts.SettingsTitleContent;
        protected override string HeaderUrl => witInspector ? witInspector.HeaderUrl : base.HeaderUrl;

        // VLog log level
        private static int _logLevel = -1;
        private static string[] _logLevelNames;
        private static LogType[] _logLevels = new LogType[] { LogType.Log, LogType.Warning, LogType.Error };

        public virtual bool ShowWitConfiguration => true;
        public virtual bool ShowGeneralSettings => true;

        public static bool ShowTooltips
        {
            get => EditorPrefs.GetBool("VSDK::Settings::Tooltips", true);
            set => EditorPrefs.SetBool("VSDK::Settings::Tooltips", value);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (string.IsNullOrEmpty(serverToken))
            {
                serverToken = WitAuthUtility.ServerToken;
            }
            RefreshLogLevel();
            SetWitEditor();
        }

        protected virtual void SetWitEditor()
        {
            // Destroy inspector
            if (witInspector != null)
            {
                DestroyImmediate(witInspector);
                witInspector = null;
            }
            // Generate new inspector & initialize immediately
            if (witConfiguration)
            {
                witInspector = (WitConfigurationEditor)Editor.CreateEditor(witConfiguration);
                witInspector.drawHeader = false;
                witInspector.Initialize();
            }
        }

        protected override void LayoutContent()
        {
            if (ShowGeneralSettings) DrawGeneralSettings();
            if (ShowWitConfiguration) DrawWitConfigurations();
        }

        private void DrawGeneralSettings()
        {
            // VLog level
            bool updated = false;
            RefreshLogLevel();
            int logLevel = _logLevel;
            WitEditorUI.LayoutPopup(WitTexts.Texts.VLogLevelLabel, _logLevelNames, ref logLevel, ref updated);
            if (updated)
            {
                SetLogLevel(logLevel);
            }

            var showTooltips = ShowTooltips;
            WitEditorUI.LayoutToggle(new GUIContent(WitTexts.Texts.ShowTooltipsLabel), ref showTooltips, ref updated);
            if (updated) ShowTooltips = showTooltips;
        }
        
        private void DrawWitConfigurations()
        {
            // Server access token
            GUILayout.BeginHorizontal();
            bool updated = false;
            WitEditorUI.LayoutPasswordField(WitTexts.SettingsServerTokenContent, ref serverToken, ref updated);
            if (updated)
            {
                RelinkServerToken(false);
            }
            if (WitEditorUI.LayoutTextButton(WitTexts.Texts.SettingsRelinkButtonLabel))
            {
                RelinkServerToken(true);
            }
            if (WitEditorUI.LayoutTextButton(WitTexts.Texts.SettingsAddButtonLabel))
            {
                OpenConfigGenerationWindow();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(WitStyles.ButtonMargin);

            // Configuration select
            base.LayoutContent();
            // Update inspector if needed
            if (witInspector == null || witConfiguration == null || witInspector.Configuration != witConfiguration)
            {
                SetWitEditor();
            }

            // Layout configuration inspector
            if (witConfiguration && witInspector)
            {
                witInspector.OnInspectorGUI();
            }
        }

        // Apply server token
        private void RelinkServerToken(bool closeIfInvalid)
        {
            // Open Setup if Invalid
            bool invalid = !WitConfigurationUtility.IsServerTokenValid(serverToken);
            if (invalid)
            {
                // Clear if desired
                if (string.IsNullOrEmpty(serverToken))
                {
                    WitAuthUtility.ServerToken = serverToken;
                }
                // Open New & Close
                if (closeIfInvalid)
                {
                    // Generate new configuration
                    OpenConfigGenerationWindow();
                    // Close
                    Close();
                }
                return;
            }

            // Set valid server token
            WitAuthUtility.ServerToken = serverToken;
            WitConfigurationUtility.SetServerToken(serverToken);
        }

        private static void RefreshLogLevel()
        {
            if (_logLevelNames != null && _logLevelNames.Length == _logLevels.Length)
            {
                return;
            }
            List<string> logLevelOptions = new List<string>();
            foreach (var level in _logLevels)
            {
                logLevelOptions.Add(level.ToString());
            }
            _logLevelNames = logLevelOptions.ToArray();
            _logLevel = logLevelOptions.IndexOf(VLog.EditorLogLevel.ToString());
        }
        private void SetLogLevel(int newLevel)
        {
            _logLevel = Mathf.Max(0, newLevel);
            VLog.EditorLogLevel = _logLevel < _logLevels.Length ? _logLevels[_logLevel] : LogType.Log;
        }
    }
}
