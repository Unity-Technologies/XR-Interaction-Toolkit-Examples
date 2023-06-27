// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Utils;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.Configuration
{
    /// <summary>
    /// Custom editor for choosing the process configuration in the Unity game object inspector.
    /// </summary>
    [CustomEditor(typeof(RuntimeConfigurator))]
    public class RuntimeConfiguratorEditor : UnityEditor.Editor
    {
        private const string configuratorSelectedProcessPropertyName = "selectedProcessStreamingAssetsPath";

        private RuntimeConfigurator configurator;
        private SerializedProperty configuratorSelectedProcessProperty;

        private static readonly List<Type> configurationTypes;
        private static readonly string[] configurationTypeNames;

        private static List<string> processDisplayNames = new List<string> { "<none>" };

        private string defaultProcessPath;
        private static bool isDirty = true;

        static RuntimeConfiguratorEditor()
        {
#pragma warning disable 0618
            configurationTypes = ReflectionUtils.GetConcreteImplementationsOf<IRuntimeConfiguration>().Except(new[] {typeof(RuntimeConfigWrapper)}).ToList();
#pragma warning restore 0618
            configurationTypes.Sort(((type1, type2) => string.Compare(type1.Name, type2.Name, StringComparison.Ordinal)));
            configurationTypeNames = configurationTypes.Select(t => t.Name).ToArray();

            ProcessAssetPostprocessor.ProcessFileStructureChanged += OnProcessFileStructureChanged;
        }

        /// <summary>
        /// True when the process list is empty or missing.
        /// </summary>
        public static bool IsProcessListEmpty()
        {
            if(isDirty)
            {
                PopulateProcessList();
            }

            return processDisplayNames.Count == 1 && processDisplayNames[0] == "<none>";
        }

        protected void OnEnable()
        {
            configurator = target as RuntimeConfigurator;

            configuratorSelectedProcessProperty = serializedObject.FindProperty(configuratorSelectedProcessPropertyName);

            defaultProcessPath = EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory;

            // Create process path if not present.
            string absolutePath = Path.Combine(Application.streamingAssetsPath, defaultProcessPath);
            if (Directory.Exists(absolutePath) == false)
            {
                Directory.CreateDirectory(absolutePath);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Processes can change without recompile so we have to check for them.
            UpdateAvailableProcesses();

            DrawRuntimeConfigurationDropDown();

            EditorGUI.BeginDisabledGroup(IsProcessListEmpty());
            {
                DrawProcessSelectionDropDown();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Open Process Editor"))
                    {
                        GlobalEditorHandler.SetCurrentProcess(ProcessAssetUtils.GetProcessNameFromPath(configurator.GetSelectedProcess()));
                        GlobalEditorHandler.StartEditingProcess();
                    }

                    if (GUILayout.Button(new GUIContent("Show Process in Explorer...")))
                    {
                        string absolutePath = $"{new FileInfo(ProcessAssetUtils.GetProcessAssetPath(ProcessAssetUtils.GetProcessNameFromPath(configurator.GetSelectedProcess())))}";
                        EditorUtility.RevealInFinder(absolutePath);
                    }
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private static void PopulateProcessList()
        {
            List<string> processes = ProcessAssetUtils.GetAllProcesses().ToList();

            // Create dummy entry if no files are present.
            if (processes.Any() == false)
            {
                processDisplayNames.Clear();
                processDisplayNames.Add("<none>");
                return;
            }

            processDisplayNames = processes;
            processDisplayNames.Sort();
        }

        private void DrawRuntimeConfigurationDropDown()
        {
            int index = configurationTypes.FindIndex(t =>
                t.AssemblyQualifiedName == configurator.GetRuntimeConfigurationName());
            index = EditorGUILayout.Popup("Configuration", index, configurationTypeNames);
            configurator.SetRuntimeConfigurationName(configurationTypes[index].AssemblyQualifiedName);
        }

        private void DrawProcessSelectionDropDown()
        {
            int index = 0;

            string processName = ProcessAssetUtils.GetProcessNameFromPath(configurator.GetSelectedProcess());

            if (string.IsNullOrEmpty(processName) == false)
            {
                index = processDisplayNames.FindIndex(processName.Equals);
            }

            index = EditorGUILayout.Popup("Selected Process", index, processDisplayNames.ToArray());

            if (index < 0)
            {
                index = 0;
            }

            string newProcessStreamingAssetsPath = ProcessAssetUtils.GetProcessStreamingAssetPath(processDisplayNames[index]);

            if (IsProcessListEmpty() == false && configurator.GetSelectedProcess() != newProcessStreamingAssetsPath)
            {
                SetConfiguratorSelectedProcess(newProcessStreamingAssetsPath);
                GlobalEditorHandler.SetCurrentProcess(processDisplayNames[index]);
            }
        }

        private void SetConfiguratorSelectedProcess(string newPath)
        {
            configuratorSelectedProcessProperty.stringValue = newPath;
        }

        private static void OnProcessFileStructureChanged(object sender, ProcessAssetPostprocessorEventArgs args)
        {
            isDirty = true;
        }

        private void UpdateAvailableProcesses()
        {
            if (isDirty == false)
            {
                return;
            }

            PopulateProcessList();

            if (string.IsNullOrEmpty(configurator.GetSelectedProcess()))
            {
                SetConfiguratorSelectedProcess(ProcessAssetUtils.GetProcessStreamingAssetPath(processDisplayNames[0]));
                GlobalEditorHandler.SetCurrentProcess(ProcessAssetUtils.GetProcessAssetPath(configurator.GetSelectedProcess()));
            }
        }
    }
}
