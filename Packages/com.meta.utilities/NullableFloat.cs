// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Utilities
{
    [System.Serializable]
    public struct NullableFloat
    {
        [SerializeField] internal float m_value;

        public float? Value
        {
            get => float.IsNaN(m_value) ? null : m_value;
            set => m_value = value ?? float.NaN;
        }

        public static implicit operator NullableFloat(float? value) => new()
        {
            Value = value
        };
    }

#if UNITY_EDITOR


    [CustomPropertyDrawer(typeof(NullableFloat))]
    internal class NullableFloatDrawer : PropertyDrawer
    {
        private static readonly float s_toggleWidth = EditorStyles.toggle.CalcSize(GUIContent.none).x;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var floatProp = property.FindPropertyRelative(nameof(NullableFloat.m_value));
            var hadValue = new NullableFloat { m_value = floatProp.floatValue }.Value.HasValue;

            var toggleRect = position;
            toggleRect.xMax = toggleRect.xMin + EditorGUIUtility.labelWidth + s_toggleWidth;
            if (EditorGUI.Toggle(toggleRect, label, hadValue))
            {
                var floatRect = position;
                floatRect.xMin = toggleRect.xMax;
                floatProp.floatValue = hadValue ? EditorGUI.FloatField(floatRect, floatProp.floatValue) : 0.0f;
            }
            else
            {
                floatProp.floatValue = ((NullableFloat)null).m_value;
            }

            EditorGUI.EndProperty();
        }
    }

#endif
}
