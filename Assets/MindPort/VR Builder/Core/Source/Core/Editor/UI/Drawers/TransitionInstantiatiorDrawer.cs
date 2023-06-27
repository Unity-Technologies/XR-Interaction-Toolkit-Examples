// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Draws a dropdown button with all <see cref="InstantiationOption{IBehavior}"/> in the project, and creates a new instance of choosen behavior on click.
    /// </summary>
    [InstantiatorProcessDrawer(typeof(ITransition))]
    internal class TransitionInstantiatiorDrawer : AbstractInstantiatorDrawer<IBehavior>
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            EditorGUI.DrawRect(new Rect(0, rect.y, rect.width + 8, 1), new Color(26f / 256f, 26f / 256f, 26f / 256f));

            rect = new Rect(rect.x, rect.y - 5, rect.width, rect.height);
            if (EditorDrawingHelper.DrawAddButton(ref rect, "Add Transition"))
            {
                ChangeValue(() => EntityFactory.CreateTransition(), () => currentValue, changeValueCallback);
            }

            return rect;
        }
    }
}
