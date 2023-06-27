// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for `System.Enum` members.
    /// </summary>
    [DefaultProcessDrawer(typeof(Enum))]
    internal class EnumDrawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            Enum oldValue = (Enum)currentValue;

            Enum newValue;

            if (currentValue.GetType().GetAttributes(true).Any(atttribute => atttribute is FlagsAttribute))
            {
                newValue = EditorGUI.EnumFlagsField(rect, label, oldValue);
            }
            else
            {
                newValue = EditorGUI.EnumPopup(rect, label, oldValue);
            }

            if (newValue.Equals(oldValue))
            {
                return rect;
            }

            ChangeValue(() => newValue, () => oldValue, changeValueCallback);

            return rect;
        }
    }
}
