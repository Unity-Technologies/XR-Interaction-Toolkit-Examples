// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor
{
    /// <summary>
    /// A class which detects if the project is going to be saved and informs the <seealso cref="GlobalEditorHandler"/> about it.
    /// </summary>
    internal class ProcessAssetProcessor : UnityEditor.AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            GlobalEditorHandler.ProjectIsGoingToSave();
            return paths;
        }
    }
}
