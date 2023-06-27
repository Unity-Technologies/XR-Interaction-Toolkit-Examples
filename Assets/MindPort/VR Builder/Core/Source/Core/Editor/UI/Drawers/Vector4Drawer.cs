// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for `Vector4`.
    /// </summary>
    [DefaultProcessDrawer(typeof(Vector4))]
    internal class Vector4Drawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight * 2f + 2f;

            Vector4 newValue = EditorGUI.Vector4Field(rect, label, (Vector4)currentValue);

            if (newValue != (Vector4)currentValue)
            {
                ChangeValue(() => newValue, () => currentValue, changeValueCallback);
            }

            return rect;
        }
    }
}
