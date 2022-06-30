using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DigitalOpus.MB.MBEditor;

namespace DigitalOpus.MB.MBEditor
{
    /// <summary>
    /// Draws rows for the PrafabPairs in the Switch Prefabs In Scene Window.
    /// </summary>
    [CustomPropertyDrawer(typeof(MB_ReplacePrefabsSettings.PrefabPair))]
    public class MB_PrefabPairPropertyDrawer : PropertyDrawer
    {
        private GUIStyle redFont = new GUIStyle();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float xx = position.x;
            float yy = position.y;
            float ww = 15f;
            float hh = position.height;

            Rect enabledRect = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);
            xx += ww;
            ww = 50;
            Rect srcLabel = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);
            xx += ww;
            ww = 200;
            Rect sourceRect = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);
            xx += ww;
            ww = 50;
            Rect targetLabel = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);
            xx += ww;
            ww = 200;
            Rect targetRect = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(enabledRect, property.FindPropertyRelative("enabled"), GUIContent.none);
            EditorGUI.LabelField(srcLabel, "Source:");
            EditorGUI.PropertyField(sourceRect, property.FindPropertyRelative("srcPrefab"), GUIContent.none);
            EditorGUI.LabelField(targetLabel, "Target:");
            EditorGUI.PropertyField(targetRect, property.FindPropertyRelative("targPrefab"), GUIContent.none);

            SerializedProperty errorsProp = property.serializedObject.FindProperty(property.propertyPath + ".objsWithErrors");
            if (errorsProp.arraySize > 0)
            {
                redFont.normal.textColor = Color.red;
                for (int i = 0; i < errorsProp.arraySize; i++)
                {
                    xx = position.x;
                    yy = position.y + (1 + i) * EditorGUIUtility.singleLineHeight;
                    ww = 100;
                    SerializedProperty errProp = errorsProp.GetArrayElementAtIndex(i);
                    GameObject obj = (GameObject)errProp.FindPropertyRelative("errorObj").objectReferenceValue;
                    string errStr = errProp.FindPropertyRelative("error").stringValue;
                    Rect buttonRect = new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(buttonRect, "Select"))
                    {
                        if (obj != null) Selection.activeGameObject = obj;
                    }
                    xx += ww;
                    ww = 500;
                    EditorGUI.LabelField(new Rect(xx, yy, ww, EditorGUIUtility.singleLineHeight), errStr, redFont);
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty errorsProp = property.serializedObject.FindProperty(property.propertyPath + ".objsWithErrors");
            return base.GetPropertyHeight(property, label) + errorsProp.arraySize * EditorGUIUtility.singleLineHeight;
        }
    }
}
