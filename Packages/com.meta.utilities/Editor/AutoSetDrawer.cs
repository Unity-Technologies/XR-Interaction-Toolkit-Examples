// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace Meta.Utilities.Editor
{
    [CustomPropertyDrawer(typeof(AutoSetAttribute))]
    [CustomPropertyDrawer(typeof(AutoSetFromChildrenAttribute))]
    [CustomPropertyDrawer(typeof(AutoSetFromParentAttribute))]
    internal class AutoSetDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _ = AutoSetPostprocessor.DoAutoSet(property, true);

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
    }
}
