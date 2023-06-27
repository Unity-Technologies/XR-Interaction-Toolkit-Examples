// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard page which handles the process scene setup.
    /// </summary>
    internal class ProcessSceneSetupPage : WizardPage
    {
        private const int MaxProcessNameLength = 40;
        private const int MinHeightOfInfoText = 30;

        public static ISceneSetupConfiguration Configuration { get; set; }

        [SerializeField]
        private bool useCurrentScene = false;

        [SerializeField]
        private bool createNewScene = true;

        [SerializeField]
        private bool createNewProcess = true;

        [SerializeField]
        private bool setupScene = true;

        [SerializeField]
        private string processName = "My VR Process";

        [SerializeField]
        private string lastCreatedProcess = null;

        [SerializeField]
        private int selectedIndex = 0;

        private readonly ISceneSetupConfiguration[] configurations;

        private readonly GUIContent infoContent;
        private readonly GUIContent warningContent;

        public ProcessSceneSetupPage() : base("Setup Process")
        {
            infoContent = EditorGUIUtility.IconContent("console.infoicon.inactive.sml");
            warningContent = EditorGUIUtility.IconContent("console.warnicon.sml");

            configurations = ReflectionUtils.GetConcreteImplementationsOf<ISceneSetupConfiguration>()
                .Select(type => ReflectionUtils.CreateInstanceOfType(type))
                .Cast<ISceneSetupConfiguration>()
                .OrderBy(config => config.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public override void Draw(Rect window)
        {
            GUILayout.BeginArea(window);

            GUILayout.Label("Setup Process", BuilderEditorStyles.Title);

            createNewProcess = GUILayout.Toggle(createNewProcess, "Create a new process", BuilderEditorStyles.Toggle);
            setupScene = GUILayout.Toggle(setupScene, "Setup the scene for VR Builder", BuilderEditorStyles.Toggle);

            if(createNewProcess && !setupScene)
            {
                EditorGUILayout.HelpBox("The new process will not work unless the scene is set up for VR Builder. Proceed only if you mean to add a new process " +
                    "to an already configured scene.", MessageType.Warning);
            }

            GUILayout.Space(16);

            if (createNewProcess)
            {
                GUILayout.Label("Name of your VR Process", BuilderEditorStyles.Header);
                processName = BuilderGUILayout.DrawTextField(processName, MaxProcessNameLength, GUILayout.Width(window.width * 0.7f));
                GUI.enabled = true;

                if (ProcessAssetUtils.CanCreate(processName, out string errorMessage) == false && lastCreatedProcess != processName)
                {
                    GUIContent processWarningContent = warningContent;
                    processWarningContent.text = errorMessage;

                    GUILayout.Label(processWarningContent, BuilderEditorStyles.Label, GUILayout.MinHeight(MinHeightOfInfoText));

                    CanProceed = false;
                }
                else
                {
                    GUILayout.Space(MinHeightOfInfoText + BuilderEditorStyles.BaseIndent);
                    CanProceed = true;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(BuilderEditorStyles.Indent);
                GUILayout.BeginVertical();
                bool isCreateNewScene = GUILayout.Toggle(createNewScene, "Create a new scene", BuilderEditorStyles.RadioButton);
                if (createNewScene == false && isCreateNewScene)
                {
                    createNewScene = true;
                    useCurrentScene = false;
                }

                bool isUseCurrentScene = GUILayout.Toggle(useCurrentScene, "Take my current scene", BuilderEditorStyles.RadioButton);
                if (useCurrentScene == false && isUseCurrentScene)
                {
                    useCurrentScene = true;
                    createNewScene = false;
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();

                if (createNewScene)
                {
                    GUIContent helpContent;
                    string sceneInfoText = "Scene will have the same name as the process.";
                    if (SceneSetupUtils.SceneExists(processName))
                    {
                        sceneInfoText += " Scene already exists";
                        CanProceed = false;
                        helpContent = warningContent;
                    }
                    else
                    {
                        helpContent = infoContent;
                    }

                    helpContent.text = sceneInfoText;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(BuilderEditorStyles.Indent);
                        EditorGUILayout.LabelField(helpContent, BuilderEditorStyles.Label, GUILayout.MinHeight(MinHeightOfInfoText));
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(16);
            }

            CanProceed |= createNewProcess == false;

            if (setupScene)
            {
                GUILayout.Label("Select a scene configuration", BuilderEditorStyles.Header);

                selectedIndex = EditorGUILayout.Popup(selectedIndex, configurations.Select(config => config.Name).ToArray(), GUILayout.Width(200));

                EditorGUILayout.HelpBox(configurations[selectedIndex].Description, MessageType.Info);
            }

            BuilderGUILayout.DrawLink("The multi user feature is available to Pro users and above. Discover more here!", "https://www.mindport.co/vr-builder/pricing", BuilderEditorStyles.IndentLarge);

            GUILayout.EndArea();
        }

        /// <inheritdoc />
        public override void Apply()
        {
            if (processName == lastCreatedProcess)
            {
                return;
            }

            if (createNewProcess && useCurrentScene == false)
            {
                SceneSetupUtils.CreateNewScene(processName);
            }

            if(setupScene)
            {
                ProcessSceneSetup.Run(configurations[selectedIndex]);
            }

            if (createNewProcess)
            {
                SceneSetupUtils.SetupProcess(processName);
            }

            lastCreatedProcess = processName;
            EditorWindow.FocusWindowIfItsOpen<WizardWindow>();
        }
    }
}
