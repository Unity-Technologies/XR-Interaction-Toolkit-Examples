/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;

namespace Meta.WitAi.CallbackHandlers
{
    [CustomEditor(typeof(SimpleIntentHandler))]
    public class SimpleIntentHandlerEditor : WitIntentMatcherEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            _fieldGUI.onAdditionalGuiLayout = OnInspectorAdditionalGUI;
        }
        private void OnInspectorAdditionalGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            var eventProperty = serializedObject.FindProperty("onIntentTriggered");
            EditorGUILayout.PropertyField(eventProperty);
        }
    }
}
