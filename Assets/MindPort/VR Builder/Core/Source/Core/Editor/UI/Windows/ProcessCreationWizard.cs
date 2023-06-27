// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Core.Configuration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// Wizard for process creation and management.
    /// </summary>
    internal class ProcessCreationWizard : EditorWindow
    {
        private static ProcessCreationWizard window;

        // ProcessCreationWizard is obsolete and was replaced by BuilderSetupWizard
#if !UNITY_2019_4_OR_NEWER || UNITY_EDITOR_OSX
        [MenuItem("Tools/VR Builder/Create Process in current scene...", false, 1)]
#endif
        private static void ShowWizard()
        {
            if (window == null)
            {
                ProcessCreationWizard[] openedProcessWizards = Resources.FindObjectsOfTypeAll<ProcessCreationWizard>();

                if (openedProcessWizards.Length > 1)
                {
                    for (int i = 1; i < openedProcessWizards.Length; i++)
                    {
                        openedProcessWizards[i].Close();
                    }

                    Debug.LogWarning("There were more than one create process windows open. This should not happen. The redundant windows were closed.");
                }

                window = openedProcessWizards.Length > 0 ? openedProcessWizards[0] : GetWindow<ProcessCreationWizard>();
            }

            window.Show();
            window.Focus();
        }

        private string processName;
        private Vector2 scrollPosition;
        private string errorMessage;

        private void OnGUI()
        {
            // Magic number.
            minSize = new Vector2(420f, 320f);
            titleContent = new GUIContent("Process Wizard");

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            Rect rect = GUILayoutUtility.GetRect(position.width, 150, GUI.skin.box);
            GUI.DrawTexture(rect, LogoEditorHelper.GetCompanyLogoTexture(LogoStyle.Icon), ScaleMode.ScaleToFit);

            if (RuntimeConfigurator.Exists == false)
            {
                EditorGUILayout.HelpBox("The current scene is not a process scene. No process can be created. To automatically setup the scene, select \"Tools > VR Builder > Setup Process Scene\".", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(RuntimeConfigurator.Exists == false);
            EditorGUILayout.LabelField("<b>Create a new process.</b>", labelStyle);

            processName = EditorGUILayout.TextField(new GUIContent("Process Name", "Set a file name for the new process."), processName);

            EditorGUILayout.LabelField("The new process will be set for the current scene.");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            // ReSharper disable once InvertIf
            if (GUILayout.Button("Create", GUILayout.Width(128), GUILayout.Height(32)))
            {
                if (ProcessAssetUtils.CanCreate(processName, out errorMessage))
                {
                    ProcessAssetManager.Import(EntityFactory.CreateProcess(processName));
                    RuntimeConfigurator.Instance.SetSelectedProcess(ProcessAssetUtils.GetProcessStreamingAssetPath(processName));
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    GlobalEditorHandler.SetCurrentProcess(processName);
                    GlobalEditorHandler.StartEditingProcess();

                    Close();
                }
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(errorMessage) == false)
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }
    }
}
