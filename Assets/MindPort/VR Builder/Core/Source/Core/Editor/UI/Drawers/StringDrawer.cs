// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for string members.
    /// </summary>
    [DefaultProcessDrawer(typeof(string))]
    internal class StringDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            string stringValue = (string)currentValue;
            string newValue = EditorGUI.TextField(rect, label, stringValue);

            if (stringValue != newValue)
            {
                ChangeValue(() => newValue, () => stringValue, changeValueCallback);
            }

            return rect;
        }
    }
}
