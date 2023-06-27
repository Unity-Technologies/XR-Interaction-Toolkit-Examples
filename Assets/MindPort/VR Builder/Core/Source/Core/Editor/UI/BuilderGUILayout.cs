// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VRBuilder.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// Layout extension for VR Builder.
    /// </summary>
    public static class BuilderGUILayout
    {
        /// <summary>
        /// Draws a clickable link which opens a website.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="url">url to be opened inside the browser</param>
        /// <param name="indent">Intend on the left</param>
        public static void DrawLink(string text, string url, int indent = BuilderEditorStyles.Indent)
        {
            DrawLink(text, () =>
            {
                try
                {
                    Process.Start(url);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }, indent);
        }

        /// <summary>
        /// Draws a clickable link which looks like a hyperlink.
        /// </summary>
        /// <param name="text">Text to be displayed</param>
        /// <param name="action">action done on click</param>
        /// <param name="indent">Intend on the left</param>
        public static void DrawLink(string text, Action action, int indent = BuilderEditorStyles.Indent)
        {
            if (GUILayout.Button(text, BuilderEditorStyles.ApplyPadding(BuilderEditorStyles.Link, indent)))
            {
                action.Invoke();
            }

            Rect buttonRect = GUILayoutUtility.GetLastRect();
            GUI.Label(new Rect(buttonRect.x, buttonRect.y + 1, buttonRect.width, buttonRect.height), new String('_', 256), BuilderEditorStyles.ApplyPadding(BuilderEditorStyles.Link, indent));
            EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
        }

        public static string DrawTextField(string content, int charLimit = -1, params GUILayoutOption[] options)
        {
            return DrawTextField(content, charLimit, 0, options);
        }

        public static string DrawTextField(string content, int charLimit = -1, int indent = 0, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal();
            {
                GUIStyle style = BuilderEditorStyles.TextField;
                if (indent != 0)
                {
                    style = BuilderEditorStyles.ApplyPadding(style, indent);
                }

                content = GUILayout.TextField(content, charLimit, style, options);

                Rect textFieldRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(textFieldRect, MouseCursor.Text);
                if (charLimit > 0)
                {
                    GUILayout.Label($"{content.Length}/{charLimit}");
                }
            }
            GUILayout.EndHorizontal();
            return content;
        }

        public static T DrawToggleGroup<T>(T selection, List<T> entries, List<string> content, List<T> disabledEntries)
        {
            return DrawToggleGroup(selection, entries, content.Select(str => new GUIContent(str)).ToList(), disabledEntries);
        }

        public static T DrawToggleGroup<T>(T selection, List<T> entries, List<GUIContent> content, List<T> disabledEntries)
        {
            bool isDisabled;

            for (int i = 0; i < entries.Count; i++)
            {
                isDisabled = disabledEntries.Contains(entries[i]);

                EditorGUI.BeginDisabledGroup(isDisabled);
                if (GUILayout.Toggle(entries[i].Equals(selection), content[i], BuilderEditorStyles.RadioButton))
                {
                    if (!selection.Equals(entries[i]))
                    {
                        selection = entries[i];
                    }
                }
                EditorGUI.EndDisabledGroup();
            }

            return selection;
        }
    }
}
