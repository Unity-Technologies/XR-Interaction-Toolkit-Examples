using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.Compilation;
using VRBuilder.Editor.Settings;

namespace VRBuilder.Editor.UI
{
    internal class InteractionComponentSettingsSection : IProjectSettingsSection
    {
        public string Title { get; } = "Interaction Component";
        public Type TargetPageProvider { get; } = typeof(BuilderSettingsProvider);
        public int Priority { get; } = 500;

        public void OnGUI(string searchContext)
        {
            InteractionComponentSettings config = InteractionComponentSettings.Instance;

            GUILayout.Label("You might want to disable the built-in interaction component in order to use a custom one or a partner integration.\n" +
                "More interaction components, such as integrations with our partners, are avaliable on our Add-ons and Integrations page.", BuilderEditorStyles.Paragraph);
            BuilderGUILayout.DrawLink("Add-ons and Integrations", "https://www.mindport.co/vr-builder-add-ons-and-integrations", BuilderEditorStyles.Indent);

            EditorGUI.BeginChangeCheck();

            config.EnableXRInteractionComponent = GUILayout.Toggle(config.EnableXRInteractionComponent, "Enable built-in XR Interaction Component", BuilderEditorStyles.Toggle);

            if (EditorGUI.EndChangeCheck())
            {
                InteractionComponentSettings.Instance.Save();
                CompilationPipeline.RequestScriptCompilation();
            }
        }
    }
}
