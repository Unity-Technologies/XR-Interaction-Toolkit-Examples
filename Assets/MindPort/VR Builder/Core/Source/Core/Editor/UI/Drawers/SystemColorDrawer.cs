// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for `System.Color` members.
    /// </summary>
    [DefaultProcessDrawer(typeof(Color))]
    internal class SystemColorDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            Color color = (Color)currentValue;
            Color newColor = EditorGUI.ColorField(rect, label, color);

            if (color != newColor)
            {
                ChangeValue(() => newColor, () => color, changeValueCallback);
            }

            return rect;
        }
    }
}
