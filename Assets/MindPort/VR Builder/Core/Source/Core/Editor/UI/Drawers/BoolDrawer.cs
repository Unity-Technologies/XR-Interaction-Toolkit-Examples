// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for boolean members.
    /// </summary>
    [DefaultProcessDrawer(typeof(bool))]
    internal class BoolDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            bool oldValue = (bool)currentValue;
            bool newValue = EditorGUI.ToggleLeft(rect, label, oldValue);

            if (oldValue != newValue)
            {
                ChangeValue(() => newValue, () => oldValue, changeValueCallback);
            }

            return rect;
        }
    }
}
