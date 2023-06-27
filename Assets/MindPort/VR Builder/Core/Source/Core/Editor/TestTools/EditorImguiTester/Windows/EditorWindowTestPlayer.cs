// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace VRBuilder.Editor.TestTools
{
    /// <summary>
    /// Utility window which sends given sequence of events to another window.
    /// </summary>
    internal static class EditorWindowTestPlayer
    {
        /// <summary>
        /// Start sending <paramref name="recordedActions"/> to the <see cref="window"/> and invoke <paramref name="finishedCallback"/> when done.
        /// </summary>
        public static void StartPlayback(EditorWindow window, IList<UserAction> recordedActions)
        {
            foreach (UserAction action in recordedActions)
            {
                TestableEditorElements.StartPlayback(action.PrepickedSelections);
                window.RepaintImmediately();
                window.SendEvent(action.Event);
                TestableEditorElements.StopPlayback();
            }
        }
    }
}
