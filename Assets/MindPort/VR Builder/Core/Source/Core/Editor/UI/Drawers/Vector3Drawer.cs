// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Default drawer for `Vector3`.
    /// </summary>
    [DefaultProcessDrawer(typeof(Vector3))]
    internal class Vector3Drawer : AbstractDrawer
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight * 2f + 2f;
            Vector3 newValue = EditorGUI.Vector3Field(rect, label, (Vector3) currentValue);

            if (newValue != (Vector3) currentValue)
            {
                ChangeValue(() => newValue, () => currentValue, changeValueCallback);
            }

            return rect;
        }
    }
}
