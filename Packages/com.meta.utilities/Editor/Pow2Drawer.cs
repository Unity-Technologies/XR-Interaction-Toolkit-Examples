// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;
using UnityEngine;

namespace Meta.Utilities
{
    /// <summary>
    /// Property drawer fot the pow2 attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(Pow2Attribute))]
    public class Pow2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var maxValue = (attribute as Pow2Attribute).MaxValue;

            var valueCount = (int)Mathf.Log(maxValue, 2) + 1;
            var values = new int[valueCount];
            var valueNames = new GUIContent[valueCount];

            for (var i = 0; i < valueCount; i++)
            {
                values[i] = 1 << i;
                valueNames[i] = new GUIContent((1 << i).ToString());
            }

            EditorGUI.IntPopup(position, property, valueNames, values);
        }
    }
}