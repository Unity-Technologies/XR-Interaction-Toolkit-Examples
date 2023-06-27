// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using VRBuilder.Core;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Editor.ProcessValidation;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Drawer for values implementing INameable interface.
    /// Instead of drawing a plain text as a label, it draws a TextField with the name.
    /// </summary>
    [DefaultProcessDrawer(typeof(INamedData))]
    public class NameableDrawer : ObjectDrawer
    {
        /// <inheritdoc />
        protected override float DrawLabel(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            INamedData nameable = currentValue as INamedData;

            List<EditorReportEntry> reports = GetValidationReports(currentValue);
            if (reports.Count > 0)
            {
                Rect warningRect = rect;
                warningRect.width = 20;
                rect.x += 20;
                GUI.Label(warningRect, AddValidationInformation(new GUIContent(), reports));
            }

            IRenameableData renameable = nameable as IRenameableData;

            if(renameable != null)
            {
                DrawRenameable(rect, renameable, changeValueCallback);
            }
            else
            {
                DrawName(rect, nameable);
            }

            return rect.height;
        }

        private void DrawRenameable(Rect rect, IRenameableData renameable, Action<object> changeValueCallback)
        {
            Rect nameRect = rect;
            nameRect.width = EditorGUIUtility.labelWidth;
            Rect typeRect = rect;
            typeRect.x += EditorGUIUtility.labelWidth;
            typeRect.width -= EditorGUIUtility.labelWidth;

            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            string newName = EditorGUI.DelayedTextField(nameRect, renameable.Name, textFieldStyle);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset(4, 0, 0, 0)
            };
            EditorGUI.LabelField(typeRect, GetTypeNameLabel(renameable, renameable.GetType()), labelStyle);

            if (newName != renameable.Name)
            {
                string oldName = renameable.Name;
                renameable.SetName(newName);
                ChangeValue(() =>
                    {
                        renameable.SetName(newName);
                        return renameable;
                    },
                    () =>
                    {
                        renameable.SetName(oldName);
                        return renameable;
                    }, changeValueCallback);
            }
        }

        private void DrawName(Rect rect, INamedData nameable)
        {
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset(4, 0, 0, 0)
            };

            string label = nameable.Name;
            if(string.IsNullOrEmpty(label))
            {
                EditorGUI.LabelField(rect, GetTypeNameLabel(nameable, nameable.GetType()), labelStyle);
            }
            else
            {
                EditorGUI.LabelField(rect, new GUIContent(label), labelStyle);
            }
        }
    }
}
