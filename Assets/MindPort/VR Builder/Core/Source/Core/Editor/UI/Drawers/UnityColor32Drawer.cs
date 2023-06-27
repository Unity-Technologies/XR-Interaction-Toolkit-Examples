// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for `UnityEngine.Color32` member.
    /// </summary>
    [DefaultProcessDrawer(typeof(Color32))]
    internal class UnityColor32Drawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            Color32 color = (Color32)currentValue;
            Color32 newColor = EditorGUI.ColorField(rect, label, color);
            if ((Color)newColor != color)
            {
                ChangeValue(() => newColor, () => color, changeValueCallback);
            }

            return rect;
        }
    }
}
