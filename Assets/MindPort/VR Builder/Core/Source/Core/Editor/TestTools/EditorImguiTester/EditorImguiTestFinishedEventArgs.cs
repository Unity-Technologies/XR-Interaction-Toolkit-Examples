// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Event args for event which is fired when a <see cref="IEditorImguiTest"/> test finishes its execution.
    /// </summary>
    internal class EditorImguiTestFinishedEventArgs : EventArgs
    {
        /// <summary>
        /// Result from the last <see cref="IEditorImguiTest"/>.
        /// </summary>
        public TestState Result { get; private set; }

        public EditorImguiTestFinishedEventArgs(TestState result)
        {
            Result = result;
        }
    }
}
