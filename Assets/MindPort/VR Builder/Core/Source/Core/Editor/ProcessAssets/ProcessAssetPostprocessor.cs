// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Linq;
using UnityEditor;

namespace VRBuilder.Editor
{
    /// <summary>
    /// Monitors process files added or removed from the project.
    /// </summary>
    internal class ProcessAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Raised when a process file is added, removed or moved from the process folder.
        /// </summary>
        public static event EventHandler<ProcessAssetPostprocessorEventArgs> ProcessFileStructureChanged;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (ProcessFileStructureChanged != null &&
                importedAssets.Concat(deletedAssets)
                    .Concat(movedAssets)
                    .Concat(movedFromAssetPaths)
                    .Any(ProcessAssetUtils.IsValidProcessAssetPath))
            {
                ProcessFileStructureChanged.Invoke(null, new ProcessAssetPostprocessorEventArgs());
            }
        }
    }
}
