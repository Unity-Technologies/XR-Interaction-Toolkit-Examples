// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI
{
    [InitializeOnLoad]
    internal static class EditorColorUtils
    {
        private static Color ModeTint { get; set; }
        private static Color DefaultColor { get; set; }
        private static Color DefaultBackgroundColor { get; set; }

        static EditorColorUtils()
        {
            DefaultColor = GUI.color;
            DefaultBackgroundColor = GUI.backgroundColor;
            ModeTint = Color.white;

            EditorApplication.playModeStateChanged += LogPlayModeState;
        }

        private static void LogPlayModeState(PlayModeStateChange mode)
        {
            if (mode == PlayModeStateChange.EnteredEditMode)
            {
                DefaultColor = GUI.color;
                DefaultBackgroundColor = GUI.backgroundColor;
                ModeTint = Color.white;
            }
            else if (mode == PlayModeStateChange.EnteredPlayMode)
            {
                DefaultColor = GUI.color;
                DefaultBackgroundColor = GUI.backgroundColor;
                ModeTint = ParseTintString(EditorPrefs.GetString("Playmode tint"));
            }
        }

        private static Color ParseTintString(string tint)
        {
            string[] token = tint.Split(';');
            return new Color
            (
                float.Parse(token[1], CultureInfo.InvariantCulture),
                float.Parse(token[2], CultureInfo.InvariantCulture),
                float.Parse(token[3], CultureInfo.InvariantCulture),
                float.Parse(token[4], CultureInfo.InvariantCulture)
            );
        }

        public static void SetTransparency(float value)
        {
            GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, value);
        }

        public static void SetTint(Color tint)
        {
            GUI.color = tint * ModeTint;
        }

        public static void ResetColor()
        {
            GUI.color = DefaultColor * ModeTint;
        }

        public static void ResetBackgroundColor()
        {
            GUI.backgroundColor = DefaultBackgroundColor;
        }

        public static void SetBackgroundColor(Color color)
        {
            GUI.backgroundColor = color;
        }
    }
}
