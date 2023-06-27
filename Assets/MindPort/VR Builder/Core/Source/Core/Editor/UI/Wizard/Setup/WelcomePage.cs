// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Wizard
{
    internal class WelcomePage : WizardPage
    {
        public WelcomePage() : base("Welcome")
        {

        }

        public override void Draw(Rect window)
        {
            GUILayout.BeginArea(window);
                GUILayout.Label("Welcome to VR Builder", BuilderEditorStyles.Title);
                GUILayout.Label("We want to get you started with VR Builder as fast as possible.\nThis Wizard guides you through the process.", BuilderEditorStyles.Paragraph);
            GUILayout.EndArea();
        }
    }
}
