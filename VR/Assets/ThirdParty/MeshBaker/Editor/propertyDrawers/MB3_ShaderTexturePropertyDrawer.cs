using UnityEditor;
using UnityEngine;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    [CustomPropertyDrawer(typeof(ShaderTextureProperty))]
    public class MB3_ShaderTexturePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            Rect contentPosition = EditorGUI.PrefixLabel(position, label);
            if (position.height > 16f)
            {
                position.height = 16f;
                EditorGUI.indentLevel += 1;
                contentPosition = EditorGUI.IndentedRect(position);
                contentPosition.y += 18f;
            }
            contentPosition.width *= 0.75f;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("name"), GUIContent.none);
            contentPosition.x += contentPosition.width;
            contentPosition.width /= 3f;
            EditorGUIUtility.labelWidth = 50f;
            EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("isNormalMap"), new GUIContent("isBump"));
            EditorGUI.EndProperty();
        }
    }
}
