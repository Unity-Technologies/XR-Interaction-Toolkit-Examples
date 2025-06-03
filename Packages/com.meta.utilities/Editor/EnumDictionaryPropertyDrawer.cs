// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Meta.Utilities.Editor
{
    [CustomPropertyDrawer(typeof(EnumDictionary<,>))]
    internal class EnumDictionaryPropertyDrawer : PropertyDrawer
    {
        protected IEnumerable<(GUIContent label, SerializedProperty value)> GetPairs(SerializedProperty property)
        {
            var enumType = fieldInfo.FieldType.GetGenericArguments()[0];
            var names = enumType.GetEnumNames();

            var array = property.FindPropertyRelative("m_values");
            array.arraySize = names.Length; // TODO: Make this smarter if the enum changes

            foreach (var i in array.arraySize.CountTo())
            {
                var element = array.GetArrayElementAtIndex(i);
                var name = names[i];
                var label = new GUIContent(name);
                yield return (label, element);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight + (property.isExpanded ?
                GetPairs(property).Sum(pair => EditorGUI.GetPropertyHeight(pair.value, pair.label, true)) :
                0.0f);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var foldoutRect = position;
            foldoutRect.yMax = foldoutRect.yMin + EditorGUIUtility.singleLineHeight;
            position.yMin = foldoutRect.yMax;

            if (property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label))
            {
                using var scope = new EditorGUI.IndentLevelScope();

                foreach (var (elementLabel, value) in GetPairs(property))
                {
                    var rect = position;
                    rect.yMax = rect.yMin + EditorGUI.GetPropertyHeight(value, elementLabel, true);
                    _ = EditorGUI.PropertyField(rect, value, elementLabel, true);
                    position.yMin = rect.yMax;
                }
            }
        }
    }
}
