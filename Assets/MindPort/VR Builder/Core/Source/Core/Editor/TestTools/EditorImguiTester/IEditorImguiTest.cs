// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections;
using UnityEditor;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Base interface for all Editor IMGUI tests.
    /// Used internally.
    /// See <see cref="EditorImguiTest{T}"/> instead.
    /// </summary>
    internal interface IEditorImguiTest
    {
        /// <summary>
        /// Invoked when test is either fails or passes.
        /// </summary>
        event EventHandler<EditorImguiTestFinishedEventArgs> Finished;

        /// <summary>
        /// Prerequisites of the test.
        /// </summary>
        string GivenDescription { get; }

        /// <summary>
        /// What has to be done by user.
        /// </summary>
        string WhenDescription { get; }

        /// <summary>
        /// Expected result.
        /// </summary>
        string ThenDescription { get; }

        /// <summary>
        /// Path to the file with recorded user actions.
        /// It should start with assets and forward slashes ("/") have to be used as path separators.
        /// </summary>
        string PathToRecordedActions { get; }

        /// <summary>
        /// Used internally. <see cref="EditorImguiTest{T}"/>.
        /// </summary>
        IEnumerator Test();

        /// <summary>
        /// Used internally. <see cref="EditorImguiTest{T}"/>.
        /// </summary>
        void Teardown();

        /// <summary>
        /// Used internally. <see cref="EditorImguiTest{T}"/>.
        /// </summary>
        EditorWindow BaseGiven();

        /// <summary>
        /// Used internally. <see cref="EditorImguiTest{T}"/>.
        /// </summary>
        void BaseThen(EditorWindow window);
    }
}
