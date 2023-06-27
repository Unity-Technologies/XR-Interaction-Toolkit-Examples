/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Reflection;
using Meta.WitAi.Utilities;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Drawers
{
    [CustomPropertyDrawer(typeof(DynamicRangeAttribute))]
    public class DynamicRangeAttributeDrawer : PropertyDrawer
    {
        private Object _targetObject;
        private float _min;
        private float _max;
        private PropertyInfo _rangePropertyField;
        private object _parentValue;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.floatValue;
            var attr = attribute as DynamicRangeAttribute;
            var parentPropertyName =
                property.propertyPath.Substring(0, property.propertyPath.IndexOf("."));
            var parentProperty = property.serializedObject.FindProperty(parentPropertyName);

            var targetObject = property.serializedObject.targetObject;
            if(targetObject != _targetObject)
            {
                _targetObject = targetObject;
                var targetObjectClassType = targetObject.GetType();
                var field = targetObjectClassType.GetField(parentProperty.propertyPath,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (null != field)
                {
                    _parentValue = field.GetValue(targetObject);
                    var parentType = _parentValue.GetType();
                    _rangePropertyField = parentType.GetProperty(attr.RangeProperty,
                        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                }
            }

            _min = attr.DefaultMin;
            _max = attr.DefaultMax;
            if (null != _rangePropertyField)
            {
                var range = (Vector2) _rangePropertyField.GetValue(_parentValue);
                _min = range.x;
                _max = range.y;
            }

            property.floatValue = EditorGUI.Slider(position, label, property.floatValue,
                _min, _max);

        }
    }
}
