/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data.ValueReferences;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Drawers
{
    [CustomPropertyDrawer(typeof(StringReference<>), true)]
    public class StringReferenceDrawer : PropertyDrawer
    {
        private const int buttonWidth = 20;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty stringValueProperty = property.FindPropertyRelative("stringValue");
            SerializedProperty stringObjectProperty = property.FindPropertyRelative("stringObject");

            EditorGUI.BeginProperty(position, label, property);

            // Draw the label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Draw the text field
            Rect textFieldRect = new Rect(position);
            textFieldRect.width -= buttonWidth;
            Rect objFieldRect = new Rect(position.x + textFieldRect.width, position.y, buttonWidth,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(stringObjectProperty.objectReferenceValue == null ? objFieldRect : position,
                stringObjectProperty, GUIContent.none);
            if (stringObjectProperty.objectReferenceValue == null)
            {
                stringValueProperty.stringValue = EditorGUI.TextField(textFieldRect, stringValueProperty.stringValue);
            }
            else
            {
                stringValueProperty.stringValue = ((IStringReference)stringObjectProperty.objectReferenceValue).Value;
            }

            Type targetType = fieldInfo.FieldType.BaseType.GenericTypeArguments[0];
            // Handle drag and drop
            if (Event.current.type == EventType.DragUpdated && textFieldRect.Contains(Event.current.mousePosition))
            {
                var validType = false;
                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject.GetType() == targetType)
                    {
                        validType = true;
                        break;
                    }
                }

                if (validType)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && textFieldRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.AcceptDrag();

                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject.GetType() == targetType)
                    {
                        stringObjectProperty.objectReferenceValue = draggedObject;
                        property.serializedObject.ApplyModifiedProperties();
                        break;
                    }
                }

                Event.current.Use();
            }

            EditorGUI.EndProperty();
        }
    }
}
