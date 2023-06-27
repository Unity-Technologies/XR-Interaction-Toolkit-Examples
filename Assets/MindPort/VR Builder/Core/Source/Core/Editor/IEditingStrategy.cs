// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Editor.UI.Windows;

namespace VRBuilder.Editor
{
    /// <summary>
    /// An interface for a strategy that defines how various events should be handled by the VR Builder editor.
    /// </summary>
    internal interface IEditingStrategy
    {
        /// <summary>
        /// Returns the current process.
        /// </summary>
        IProcess CurrentProcess { get; }

        IChapter CurrentChapter { get; }

        /// <summary>
        /// Invoked when a new <see cref="ProcessEditorWindow"/> was just opened.
        /// </summary>
        void HandleNewProcessWindow(ProcessEditorWindow window);

        /// <summary>
        /// Invoked when a new <see cref="IStepView"/> was just opened.
        /// </summary>
        void HandleNewStepWindow(IStepView window);

        /// <summary>
        /// Invoked when a designer has just modified the process in the editor.
        /// </summary>
        void HandleCurrentProcessModified();

        /// <summary>
        /// Invoked when a <see cref="ProcessEditorWindow"/> was closed.
        /// </summary>
        void HandleProcessWindowClosed(ProcessEditorWindow window);

        /// <summary>
        /// Invoked when a <see cref="IStepView"/> was closed.
        /// </summary>
        void HandleStepWindowClosed(IStepView window);

        /// <summary>
        /// Invoked when user wants to start working on the current process.
        /// </summary>
        void HandleStartEditingProcess();

        /// <summary>
        /// Invoked when the currently edited process was changed to a different one.
        /// </summary>
        void HandleCurrentProcessChanged(string processName);

        /// <summary>
        /// Invoked when the currently edited <see cref="IStep"/> was modified.
        /// </summary>
        void HandleCurrentStepModified(IStep step);

        /// <summary>
        /// Invoked when a designer wants to start working on a step.
        /// </summary>
        void HandleStartEditingStep();

        /// <summary>
        /// Invoked when a designer chooses a <see cref="IStep"/> to edit.
        /// </summary>
        void HandleCurrentStepChanged(IStep step);

        /// <summary>
        /// Invoked when the chapter changes, can be null.
        /// </summary>
        void HandleCurrentChapterChanged(IChapter chapter);

        /// <summary>
        /// Invoked on a request to switch to a different chapter.
        /// </summary>        
        void HandleChapterChangeRequest(IChapter chapter);

        /// <summary>
        ///  Invoked when the project is going to be unloaded (when assemblies are unloaded, when user starts or stop runtime, when scripts were modified).
        /// </summary>
        void HandleProjectIsGoingToUnload();

        /// <summary>
        /// Invoked just before Unity saves the project (either during the normal exit of the Editor application or when the designer clicks `Save Project`).
        /// </summary>
        void HandleProjectIsGoingToSave();

        /// <summary>
        /// Invoked when exiting play mode.
        /// </summary>
        void HandleExitingPlayMode();

        /// <summary>
        /// Invoked when entering play mode.
        /// </summary>
        void HandleEnterPlayMode();
    }
}
