// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for float members.
    /// </summary>
    [DefaultProcessDrawer(typeof(float))]
    internal class FloatDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            float value = (float)currentValue;
            float newValue = EditorGUI.FloatField(rect, label, value);

            // Rounding error can't take place here.
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (value != newValue)
            {
                ChangeValue(() => newValue, () => value, changeValueCallback);
            }

            return rect;
        }
    }
}
