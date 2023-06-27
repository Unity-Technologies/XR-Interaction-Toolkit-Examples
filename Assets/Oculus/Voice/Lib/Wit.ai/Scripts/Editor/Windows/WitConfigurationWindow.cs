/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Meta.WitAi.Data.Configuration;

namespace Meta.WitAi.Windows
{
    public abstract class WitConfigurationWindow : BaseWitWindow
    {
        // Configuration data
        protected int witConfigIndex = -1;
        protected WitConfiguration witConfiguration;

        protected override string HeaderUrl
        {
            get
            {
                if (witConfiguration == null)
                {
                    return "";
                }

                string appID = witConfiguration.GetApplicationId();
                if (!string.IsNullOrEmpty(appID))
                {
                    return WitTexts.GetAppURL(appID, HeaderEndpointType);
                }
                return base.HeaderUrl;
            }
        }
        protected virtual WitTexts.WitAppEndpointType HeaderEndpointType => WitTexts.WitAppEndpointType.Settings;
        protected virtual void SetConfiguration(int newConfigIndex)
        {
            witConfigIndex = newConfigIndex;
            WitConfiguration[] witConfigs = WitConfigurationUtility.WitConfigs;
            witConfiguration = witConfigs != null && witConfigIndex >= 0 && witConfigIndex < witConfigs.Length ? witConfigs[witConfigIndex] : null;
        }
        public virtual void SetConfiguration(WitConfiguration newConfiguration)
        {
            int newConfigIndex = newConfiguration == null ? -1 : Array.IndexOf(WitConfigurationUtility.WitConfigs, newConfiguration);
            if (newConfigIndex != -1)
            {
                SetConfiguration(newConfigIndex);
            }
            else
            {
                SetConfiguration(WitConfigurationUtility.WitConfigs.Length > 0 ? 0 : -1);
            }
        }

        // Reset configuration
        protected virtual void ResetConfiguration()
        {
            // Get previous config
            WitConfiguration config = witConfiguration;

            // Refresh configuration list
            WitConfigurationUtility.ReloadConfigurationData();

            // Apply configuration
            SetConfiguration(config);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorSceneManager.sceneOpened += OnSceneOpen;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            ResetConfiguration();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            EditorSceneManager.sceneOpened -= OnSceneOpen;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnSceneOpen(Scene scene, OpenSceneMode mode)
        {
            ResetConfiguration();
        }
        private void OnSceneSaved(Scene scene)
        {
            ResetConfiguration();
        }

        protected override void LayoutContent()
        {
            // Reload if config is removed
            if (witConfiguration == null && witConfigIndex != -1)
            {
                ResetConfiguration();
            }

            // Layout popup
            int index = witConfigIndex;
            WitConfigurationEditorUI.LayoutConfigurationSelect(ref index, OpenConfigGenerationWindow);
            GUILayout.Space(WitStyles.ButtonMargin);
            // Selection changed
            if (index != witConfigIndex)
            {
                SetConfiguration(index);
            }
        }
        // Generate new configuration via setup
        protected virtual void OpenConfigGenerationWindow()
        {
            WitWindowUtility.OpenSetupWindow(OnConfigGenerated);
        }
        // On configuration generated
        protected virtual void OnConfigGenerated(WitConfiguration newConfiguration)
        {
            // Apply to this settings window
            if (newConfiguration != null)
            {
                // Get index if possible
                List<WitConfiguration> configs = new List<WitConfiguration>(WitConfigurationUtility.WitConfigs);
                int newIndex = configs.IndexOf(newConfiguration);
                if (newIndex != -1)
                {
                    // Apply configuration
                    SetConfiguration(newIndex);
                }
            }

            // Open this window if needed
            WitWindowUtility.OpenConfigurationWindow();
        }
    }
}
