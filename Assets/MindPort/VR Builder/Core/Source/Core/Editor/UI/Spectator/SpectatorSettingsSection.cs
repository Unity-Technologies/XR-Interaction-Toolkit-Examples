using System;
using VRBuilder.Editor.Input;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI
{
    internal class SpectatorSettingsSection : IProjectSettingsSection
    {
        public string Title { get; } = "Spectator Settings";
        public Type TargetPageProvider { get; } = typeof(SpectatorSettingsProvider);
        public int Priority { get; } = 100;

        public void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            GUIStyle labelStyle = BuilderEditorStyles.ApplyPadding(BuilderEditorStyles.Paragraph, 0);
            GUILayout.Label("These settings help you to configure the spectator for non-VR users.", labelStyle);
            EditorGUILayout.Space();

            if (GUILayout.Button("Edit key bindings"))
            {
                InputEditorUtils.OpenKeyBindingEditor();
            }

            EditorGUILayout.Space(20f);
        }
    }
}
