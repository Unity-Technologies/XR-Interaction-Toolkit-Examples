/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Linq;
using System.Reflection;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.CallbackHandlers
{
    public class WitIntentMatcherEditor : Editor
    {
        // The matcher
        protected WitIntentMatcher _matcher;
        // Custom field gui
        protected FieldGUI _fieldGUI;

        // Intents & current intent
        private string[] _intentNames;
        private int _intentIndex;

        // On enable, setup gui
        protected virtual void OnEnable()
        {
            _matcher = target as WitIntentMatcher;

            // Setup field gui
            if (_fieldGUI == null)
            {
                _fieldGUI = new FieldGUI();
                _fieldGUI.onCustomGuiLayout = OnInspectorCustomGUI;
            }
        }
        // Inspector gui
        public override void OnInspectorGUI()
        {
            if (!_matcher.Voice)
            {
                GUILayout.Label("VoiceService component is not present in the scene. Add voice service to scene to get intent suggestions.",
                    EditorStyles.helpBox);
            }

            // Intent suggestions
            if (_matcher && _matcher.Voice && null == _intentNames)
            {
                if (_matcher.Voice is IWitRuntimeConfigProvider provider
                    && null != provider.RuntimeConfiguration
                    && provider.RuntimeConfiguration.witConfiguration)
                {
                    SetAppInfo(provider.RuntimeConfiguration.witConfiguration.GetApplicationInfo());
                }
            }

            // Layout fields
            _fieldGUI.OnGuiLayout(serializedObject);
        }
        // Set app info
        protected virtual void SetAppInfo(WitAppInfo appInfo)
        {
            if (appInfo.intents != null)
            {
                _intentNames = appInfo.intents.Select(i => i.name).ToArray();
                _intentIndex = Array.IndexOf(_intentNames, _matcher.intent);
            }
        }
        // Custom GUI
        protected virtual bool OnInspectorCustomGUI(FieldInfo fieldInfo)
        {
            // Custom layout
            if (string.Equals(fieldInfo.Name, "intent"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation Settings", EditorStyles.boldLabel);
                WitEditorUI.LayoutSerializedObjectPopup(serializedObject, "intent",
                    _intentNames, ref _intentIndex);
                return true;
            }
            // Layout intent triggered
            return false;
        }
    }
}
