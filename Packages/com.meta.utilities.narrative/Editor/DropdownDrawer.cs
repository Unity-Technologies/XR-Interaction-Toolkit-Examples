// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUI;

namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(Dropdown))]
    public class DropdownDrawer : PropertyDrawer
    {
        private SerializedProperty m_property;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (DropdownButton(position, new GUIContent(property.stringValue != "" ? property.stringValue : "Select Key"), FocusType.Passive))
            {
                var dropdown = attribute as Dropdown;
                m_property = property;
                var menu = new GenericMenu();

                foreach (var obj in Object.FindObjectsByType(dropdown.SourceType, FindObjectsSortMode.None))
                {
                    var type = obj.GetType();
                    var field = type.GetField(dropdown.FieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var str = field.GetValue(obj) as string;
                        menu.AddItem(new GUIContent(str), property.stringValue == str, () => Select(str));
                    }
                }

                menu.DropDown(position);
            }
        }

        private void Select(string data)
        {
            m_property.stringValue = data;
            _ = m_property.serializedObject.ApplyModifiedProperties();
        }
    }
}
