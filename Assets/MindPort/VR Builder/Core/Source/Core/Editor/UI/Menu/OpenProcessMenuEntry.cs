// Copyright (c) 2021 MindPort GmbH
// Licensed under the Apache License, Version 2.0

using UnityEditor;
using VRBuilder.Core.Configuration;
using VRBuilder.Editor.Configuration;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class OpenProcessMenuEntry
    {
        /// <summary>
        /// Open the Workflow Editor window.
        /// </summary>
        [MenuItem("Tools/VR Builder/Process Editor", false, 2)]
        [MenuItem("Window/VR Builder/Process Editor", false, 100)]
        private static void OpenWorkflowEditor()
        {
            GlobalEditorHandler.SetCurrentProcess(ProcessAssetUtils.GetProcessNameFromPath(RuntimeConfigurator.Instance.GetSelectedProcess()));
            GlobalEditorHandler.StartEditingProcess();
        }

        [MenuItem("Tools/VR Builder/Open Process Editor", true, 2)]
        [MenuItem("Window/VR Builder/Process Editor", true, 100)]
        private static bool ValidateOpenWorkflowEditor()
        {
            if (RuntimeConfigurator.Exists == false)
            {
                return false;
            }

            if (RuntimeConfiguratorEditor.IsProcessListEmpty())
            {
                return false;
            }

            return true;
        }
    }
}
