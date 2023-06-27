// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Draws a dropdown button with all <see cref="InstantiationOption{ICondition}"/> in the project, and creates a new instance of choosen condition on click.
    /// </summary>
    [InstantiatorProcessDrawer(typeof(ICondition))]
    internal class ConditionInstantiatorDrawer : AbstractInstantiatorDrawer<ICondition>
    {
        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(EditorConfigurator.Instance.AllowedMenuItemsSettings.GetConditionMenuOptions().Any() == false);
            if (EditorDrawingHelper.DrawAddButton(ref rect, "Add Condition"))
            {
                IList<TestableEditorElements.MenuOption> options = ConvertFromConfigurationOptionsToGenericMenuOptions(EditorConfigurator.Instance.ConditionsMenuContent, currentValue, changeValueCallback);
                TestableEditorElements.DisplayContextMenu(options);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorDrawingHelper.DrawHelpButton(ref rect))
            {
                Application.OpenURL("https://www.mindport.co/vr-builder/manual/default-conditions");
            }
            if (EditorConfigurator.Instance.AllowedMenuItemsSettings.GetConditionMenuOptions().Any() == false)
            {
                rect.y += rect.height + EditorDrawingHelper.VerticalSpacing;
                rect.width -= EditorDrawingHelper.IndentationWidth;
                EditorGUI.HelpBox(rect, "Your project does not contain any Conditions. Either create one or import a VR Builder Component.", MessageType.Error);
                rect.height += rect.height + EditorDrawingHelper.VerticalSpacing;
            }
            return rect;
        }
    }
}
