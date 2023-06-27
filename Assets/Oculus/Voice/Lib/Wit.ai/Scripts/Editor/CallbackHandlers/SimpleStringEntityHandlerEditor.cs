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
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.CallbackHandlers
{
    [CustomEditor(typeof(SimpleStringEntityHandler))]
    public class SimpleStringEntityHandlerEditor : WitIntentMatcherEditor
    {
        // Entity values
        private string[] _entityNames;
        private int _entityIndex;

        // Set app info
        protected override void SetAppInfo(WitAppInfo appInfo)
        {
            base.SetAppInfo(appInfo);
            if (appInfo.entities != null)
            {
                _entityNames = appInfo.entities.Select(i => i.name).ToArray();
                _entityIndex = Array.IndexOf(_entityNames, ((SimpleStringEntityHandler)_matcher).entity);
            }
        }
        // Custom GUI
        protected override bool OnInspectorCustomGUI(FieldInfo fieldInfo)
        {
            base.OnInspectorCustomGUI(fieldInfo);
            // Custom layout
            if (string.Equals(fieldInfo.Name, "entity"))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Entity", EditorStyles.boldLabel);
                WitEditorUI.LayoutSerializedObjectPopup(serializedObject, "entity",
                    _entityNames, ref _entityIndex);
                return true;
            }
            // Layout intent triggered
            return false;
        }
    }
}
