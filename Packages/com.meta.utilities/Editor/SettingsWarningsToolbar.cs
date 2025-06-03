// Copyright (c) Meta Platforms, Inc. and affiliates.

#if TOOLBAR_EXTENDER

using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Meta.Utilities.Editor
{
    [ExecuteInEditMode]
    public class SettingsWarningsToolbar
    {
        [InitializeOnLoadMethod]
        private static void Initialize() => ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);

        private static void OnToolbarGUI()
        {
            if (EditorUserBuildSettings.activeBuildTarget is not BuildTarget.Android)
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = new() { textColor = Color.red },
                    hover = new() { textColor = Color.red },
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label($"Build target must be set to Android.", style, GUILayout.ExpandWidth(false));
                if (GUILayout.Button("Fix", GUILayout.ExpandWidth(false)))
                {
                    _ = EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}

#endif
