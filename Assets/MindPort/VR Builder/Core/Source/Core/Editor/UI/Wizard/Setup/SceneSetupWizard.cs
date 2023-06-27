// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEditor;
using System.Collections.Generic;

namespace VRBuilder.Editor.UI.Wizard
{
    /// <summary>
    /// Wizard which guides the user through creating a new process
    /// in a new or existing scene.
    /// </summary>
    public static class SceneSetupWizard
    {
        /// <summary>
        /// Will be called when the Process Setup wizard is closed.
        /// </summary>
        public static event EventHandler<EventArgs> SetupFinished;

        [MenuItem("Tools/VR Builder/Scene Setup Wizard...", false, 0)]
        internal static void Show()
        {
            WizardWindow wizard = EditorWindow.CreateInstance<WizardWindow>();
            List<WizardPage> pages = new List<WizardPage>()
            {
                new ProcessSceneSetupPage(),
            };            

            wizard.WizardClosing += OnWizardClosing;

            wizard.Setup("VR Builder - Scene Setup Wizard", pages);
            wizard.ShowModalUtility();
        }

        private static void OnWizardClosing(object sender, EventArgs args)
        {
            ((WizardWindow)sender).WizardClosing -= OnWizardClosing;
            SetupFinished?.Invoke(sender, args);
        }
    }
}
