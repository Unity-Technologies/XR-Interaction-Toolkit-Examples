using UnityEngine;
using System;
using UnityEditor;
using VRBuilder.Core.Settings;

namespace VRBuilder.Editor.UI
{
    /// <summary>
    /// Section where the user can set up interaction preferences.
    /// </summary>
    internal class InteractionSettingsSection : IProjectSettingsSection
    {
        public string Title { get; } = "Interaction Settings";
        public Type TargetPageProvider { get; } = typeof(BuilderSettingsProvider);
        public int Priority { get; } = 250;

        public void OnGUI(string searchContext)
        {
            InteractionSettings config = InteractionSettings.Instance;

            EditorGUI.BeginChangeCheck();

            config.MakeGrabbablesKinematic = GUILayout.Toggle(config.MakeGrabbablesKinematic, "Grabbable objects have physics disabled by default", BuilderEditorStyles.Toggle);

            if (EditorGUI.EndChangeCheck())
            {
                InteractionSettings.Instance.Save();
            }
        }
    }
}
