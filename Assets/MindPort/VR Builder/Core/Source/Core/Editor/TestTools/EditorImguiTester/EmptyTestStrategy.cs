// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;
using VRBuilder.Editor.UI.Windows;
using UnityEngine;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// An editing strategy that does nothing. Use it to isolate windows logic during testing.
    /// </summary>
    internal class EmptyTestStrategy : IEditingStrategy
    {
        public IProcess CurrentProcess { get; }
        public IChapter CurrentChapter { get; private set; }

        /// <inheritdoc/>
        public void HandleNewProcessWindow(ProcessEditorWindow window)
        {
        }

        /// <inheritdoc/>
        public void HandleNewStepWindow(IStepView window)
        {
        }

        /// <inheritdoc/>
        public void HandleCurrentProcessModified()
        {
        }

        /// <inheritdoc/>
        public void HandleProcessWindowClosed(ProcessEditorWindow window)
        {
        }

        /// <inheritdoc/>
        public void HandleStepWindowClosed(IStepView window)
        {
        }

        /// <inheritdoc/>
        public void HandleStartEditingProcess()
        {
        }

        /// <inheritdoc/>
        public void HandleCurrentProcessChanged(string processName)
        {
        }

        /// <inheritdoc/>
        public void HandleCurrentStepModified(IStep step)
        {
        }

        /// <inheritdoc/>
        public void HandleStartEditingStep()
        {
        }

        /// <inheritdoc/>
        public void HandleCurrentStepChanged(IStep step)
        {
        }

        public void HandleCurrentChapterChanged(IChapter chapter)
        {
            CurrentChapter = chapter;
        }

        /// <inheritdoc/>
        public void HandleProjectIsGoingToUnload()
        {
        }

        /// <inheritdoc/>
        public void HandleProjectIsGoingToSave()
        {
        }

        /// <inheritdoc/>
        public void HandleExitingPlayMode()
        {
        }

        /// <inheritdoc/>
        public void HandleEnterPlayMode()
        {
        }

        /// <inheritdoc/>
        public void HandleChapterChangeRequest(IChapter chapter)
        {
        }
    }
}
