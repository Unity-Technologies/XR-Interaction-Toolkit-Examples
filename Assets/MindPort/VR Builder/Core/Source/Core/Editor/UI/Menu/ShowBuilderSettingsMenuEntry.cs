// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Editor.Configuration;
using UnityEditor;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class ShowBuilderSettingsMenuEntry
    {
        /// <summary>
        /// Opens VR Builder-related project settings.
        /// </summary>
        [MenuItem("Tools/VR Builder/Settings", false, 16)]
        public static void Show()
        {
            SettingsService.OpenProjectSettings("Project/VR Builder");
        }
    }
}
