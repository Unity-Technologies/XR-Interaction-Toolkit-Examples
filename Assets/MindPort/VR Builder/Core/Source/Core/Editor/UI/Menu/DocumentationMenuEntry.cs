// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class DocumentationMenuEntry
    {
        /// <summary>
        /// Allows to open the URL to Creator Documentation.
        /// </summary>
        [MenuItem("Tools/VR Builder/VR Builder Help/Manual", false, 80)]
        private static void OpenDocumentation()
        {
            Application.OpenURL("https://www.mindport.co/vr-builder/manual/introduction");
        }
    }
}
