// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Configuration;
using VRBuilder.Editor.Setup;

namespace VRBuilder.Editor
{
    /// <summary>
    /// Will setup a <see cref="RuntimeConfigurator"/> when none is existent in scene.
    /// </summary>
    internal class RuntimeConfigurationSetup : SceneSetup
    {
        public static readonly string ProcessConfigurationName = "PROCESS_CONFIGURATION";

        /// <inheritdoc/>
        public override void Setup(ISceneSetupConfiguration configuration)
        {
            if (RuntimeConfigurator.Exists == false)
            {
                GameObject obj = new GameObject(ProcessConfigurationName);
                RuntimeConfigurator configurator = obj.AddComponent<RuntimeConfigurator>();
                configurator.SetRuntimeConfigurationName(configuration.RuntimeConfigurationName);
                SceneConfiguration sceneConfiguration = obj.AddComponent<SceneConfiguration>();
                sceneConfiguration.AddWhitelistAssemblies(configuration.AllowedExtensionAssemblies);
                sceneConfiguration.DefaultConfettiPrefab = configuration.DefaultConfettiPrefab;
                Selection.activeObject = obj;
            }
        }
    }
}
