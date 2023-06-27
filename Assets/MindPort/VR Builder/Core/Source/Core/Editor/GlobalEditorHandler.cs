// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRBuilder.Core;
using VRBuilder.Editor.UI.Windows;
using VRBuilder.Core.Configuration;

namespace VRBuilder.Editor
{
    /// <summary>
    /// A class that handles interactions between Builder windows and process assets by using selected <seealso cref="IEditingStrategy"/> strategy.
    /// </summary>
    [InitializeOnLoad]
    internal static class GlobalEditorHandler
    {
        internal const string LastEditedProcessNameKey = "VRBuilder.Editors.LastEditedProcessName";

        private static IEditingStrategy strategy;

        static GlobalEditorHandler()
        {
            SetDefaultStrategy();

            string lastEditedProcessName = EditorPrefs.GetString(LastEditedProcessNameKey);
            SetCurrentProcess(lastEditedProcessName);

            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        /// <summary>
        /// Sets <see cref="DefaultEditingStrategy"/> as current strategy.
        /// </summary>
        internal static void SetDefaultStrategy()
        {
            SetStrategy(new GraphViewEditingStrategy());
        }

        /// <summary>
        /// Sets given <see cref="IEditingStrategy"/> as current strategy.
        /// </summary>
        internal static void SetStrategy(IEditingStrategy newStrategy)
        {
            strategy = newStrategy;

            if (newStrategy == null)
            {
                Debug.LogError("An editing strategy cannot be null, set to default instead.");
                SetDefaultStrategy();
            }
        }

        /// <summary>
        /// Returns the current active process, can be null.
        /// </summary>
        internal static IProcess GetCurrentProcess()
        {
            return strategy.CurrentProcess;
        }

        /// <summary>
        /// Returns the current active chapter, can be null.
        /// </summary>
        internal static IChapter GetCurrentChapter()
        {
            return strategy.CurrentChapter;
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a new <see cref="ProcessWindow"/> was just opened.
        /// </summary>
        internal static void ProcessWindowOpened(ProcessEditorWindow window)
        {
            strategy.HandleNewProcessWindow(window);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a <see cref="ProcessWindow"/> was closed.
        /// </summary>
        internal static void ProcessWindowClosed(ProcessEditorWindow window)
        {
            strategy.HandleProcessWindowClosed(window);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a new <see cref="StepWindow"/> was just opened.
        /// </summary>
        internal static void StepWindowOpened(StepWindow window)
        {
            strategy.HandleNewStepWindow(window);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a <see cref="StepWindow"/> was closed.
        /// </summary>
        internal static void StepWindowClosed(StepWindow window)
        {
            strategy.HandleStepWindowClosed(window);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when the currently edited process was changed to a different one.
        /// </summary>
        internal static void SetCurrentProcess(string processName)
        {
            strategy.HandleCurrentProcessChanged(processName);
        }

        internal static void SetCurrentChapter(IChapter chapter)
        {
            strategy.HandleCurrentChapterChanged(chapter);
        }

        internal static void RequestNewChapter(IChapter chapter)
        {
            strategy.HandleChapterChangeRequest(chapter);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when user wants to start working on the current process.
        /// </summary>
        internal static void StartEditingProcess()
        {
            strategy.HandleStartEditingProcess();
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a designer has just modified the process in the editor.
        /// </summary>
        internal static void CurrentProcessModified()
        {
            strategy.HandleCurrentProcessModified();
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when the currently edited <see cref="IStep"/> was modified.
        /// </summary>
        internal static void CurrentStepModified(IStep step)
        {
            strategy.HandleCurrentStepModified(step);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a designer chooses a <see cref="IStep"/> to edit.
        /// </summary>
        internal static void ChangeCurrentStep(IStep step)
        {
            strategy.HandleCurrentStepChanged(step);
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when a designer wants to start working on a step.
        /// </summary>
        internal static void StartEditingStep()
        {
            strategy.HandleStartEditingStep();
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> when the project is going to be unloaded (when assemblies are unloaded, when user starts or stop runtime, when scripts were modified).
        /// </summary>
        internal static void ProjectIsGoingToUnload()
        {
            strategy.HandleProjectIsGoingToUnload();
        }

        /// <summary>
        /// Notifies selected <see cref="IEditingStrategy"/> before Unity saves the project (either during the normal exit of the Editor application or when the designer clicks `Save Project`).
        /// </summary>
        internal static void ProjectIsGoingToSave()
        {
            strategy.HandleProjectIsGoingToSave();
        }

        internal static void EnterPlayMode()
        {
            strategy.HandleEnterPlayMode();
        }

        internal static void ExitPlayMode()
        {
            strategy.HandleExitingPlayMode();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (RuntimeConfigurator.Exists == false)
            {
                SetCurrentProcess(string.Empty);
                return;
            }

            string processPath = RuntimeConfigurator.Instance.GetSelectedProcess();

            if (string.IsNullOrEmpty(processPath))
            {
                SetCurrentProcess(string.Empty);
                return;
            }

            string processName = System.IO.Path.GetFileNameWithoutExtension(processPath);
            SetCurrentProcess(processName);
        }
    }
}
