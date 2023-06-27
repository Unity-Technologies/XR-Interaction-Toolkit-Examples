using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.Settings;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard page which prompts the user to download the XR Interaction Component
    /// </summary>
    internal class InteractionComponentPage : WizardPage
    {
        [SerializeField]
        private bool updateXRInteractionComponent = true;

        [SerializeField]
        private bool enableXRInteractionComponent;

        public InteractionComponentPage() : base("Interaction Component")
        {
        }

        public override void Draw(Rect window)
        {
            GUILayout.BeginArea(window);

            GUILayout.Label("Choose Interaction Component", BuilderEditorStyles.Title);

            IEnumerable<Type> interactionComponents = ReflectionUtils.GetConcreteImplementationsOf<IInteractionComponentConfiguration>();

            if (interactionComponents.Count() == 0)
            {
                HandleMissingInteractionComponent();
            }
            else if(interactionComponents.Count() == 2 && InteractionComponentSettings.Instance.EnableXRInteractionComponent)
            {
                HandleTwoInteractionComponents();
            }
            else
            {
                HandleMultipleInteractionComponents();
            }

            GUILayout.Space(16);

            GUILayout.Label("More interaction components, such as integrations with our partners, are avaliable on our Add-ons and Integrations page.", BuilderEditorStyles.Paragraph);
            BuilderGUILayout.DrawLink("Add-ons and Integrations", "https://www.mindport.co/vr-builder-add-ons-and-integrations", BuilderEditorStyles.Indent);

            GUILayout.Space(16);

            GUILayout.Label("Here you can find comprehensive guides on how to install non-default interaction components.", BuilderEditorStyles.Paragraph);
            BuilderGUILayout.DrawLink("How to setup VR Builder with Interhaptics VR Interaction Essentials", "https://www.mindport.co/vr-builder-learning-path/interhaptics-integration", BuilderEditorStyles.Indent);

            GUILayout.EndArea();
        }

        private void HandleTwoInteractionComponents()
        {
            GUILayout.Label("Multiple Interaction Components", BuilderEditorStyles.Header);



            GUILayout.Label("The following interaction components has been found in the project. It is recommended that you disable the built-in XR Interaction Component in order to have only one.", BuilderEditorStyles.Paragraph);

            IEnumerable<Type> interactionComponents = ReflectionUtils.GetConcreteImplementationsOf<IInteractionComponentConfiguration>();

            foreach (Type type in interactionComponents)
            {
                IInteractionComponentConfiguration configuration = ReflectionUtils.CreateInstanceOfType(type) as IInteractionComponentConfiguration;
                GUILayout.Label("- " + configuration.DisplayName, BuilderEditorStyles.Paragraph);
            }

            GUILayout.Space(16);

            if (GUILayout.Toggle(updateXRInteractionComponent, "Disable default XR Interaction Component and restart the wizard.", BuilderEditorStyles.RadioButton))
            {
                updateXRInteractionComponent = true;
                enableXRInteractionComponent = false;
                ShouldRestart = true;
            }

            if (GUILayout.Toggle(!updateXRInteractionComponent, "Skip for now. I know what I'm doing.", BuilderEditorStyles.RadioButton))
            {
                updateXRInteractionComponent = false;
                ShouldRestart = false;

                EditorGUILayout.HelpBox("VR Builder might not work properly if more than one interaction component is found.", MessageType.Warning);
            }
        }

        private void HandleMissingInteractionComponent()
        {
            GUILayout.Label("Missing Interaction Component", BuilderEditorStyles.Header);

            GUILayout.Label("No active interaction component has been found in the project. You can enable the default XR Interaction component (based on Unity's XR Interaction Toolkit) and VR Builder will be ready for use. If you want to install another interaction component on your own, please skip for now.", BuilderEditorStyles.Paragraph);

            GUILayout.Space(16);

            if (GUILayout.Toggle(updateXRInteractionComponent, "Enable default XR Interaction Component and restart the wizard.", BuilderEditorStyles.RadioButton))
            {
                updateXRInteractionComponent = true;
                enableXRInteractionComponent = true;
                ShouldRestart = true;
            }

            if (GUILayout.Toggle(!updateXRInteractionComponent, "Skip for now. I will install a different interaction component.", BuilderEditorStyles.RadioButton))
            {
                updateXRInteractionComponent = false;
                ShouldRestart = false;

                EditorGUILayout.HelpBox("VR Builder will not work properly until an interaction component is found.", MessageType.Warning);
            }
        }

        private void HandleMultipleInteractionComponents()
        {
            updateXRInteractionComponent = false;
            GUILayout.Label("Multiple Interaction Components", BuilderEditorStyles.Header);

            GUILayout.Label("The following interaction components have been found in the project.", BuilderEditorStyles.Paragraph);
            GUILayout.Space(16);

            IEnumerable<Type> interactionComponents = ReflectionUtils.GetConcreteImplementationsOf<IInteractionComponentConfiguration>();

            foreach(Type type in interactionComponents)
            {
                IInteractionComponentConfiguration configuration = ReflectionUtils.CreateInstanceOfType(type) as IInteractionComponentConfiguration;
                GUILayout.Label("- " + configuration.DisplayName, BuilderEditorStyles.Paragraph);
            }

            GUILayout.Space(16);
            EditorGUILayout.HelpBox("More than one interaction component may cause issues, please ensure only one is present in a given project.", MessageType.Warning);
        }

        public override void Apply()
        {
            base.Apply();

            if (updateXRInteractionComponent)
            {
                InteractionComponentSettings.Instance.EnableXRInteractionComponent = enableXRInteractionComponent;
                InteractionComponentSettings.Instance.Save();
                CompilationPipeline.RequestScriptCompilation();
            }
        }
    }
}
