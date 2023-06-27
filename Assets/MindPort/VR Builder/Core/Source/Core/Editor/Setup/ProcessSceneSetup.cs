// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using VRBuilder.Editor.Configuration;
using UnityEngine;
using VRBuilder.Editor.Setup;
using VRBuilder.Core.Utils;

namespace VRBuilder.Editor
{
    /// <summary>
    /// Can be run to setup the current scene as a process scene.
    /// </summary>
    internal static class ProcessSceneSetup
    {
        /// <summary>
        /// Fetches all implementations of <see cref="SceneSetup"/> and runs it.
        /// </summary>
        public static void Run(ISceneSetupConfiguration configuration)
        {
            // Create default save folder.
            Directory.CreateDirectory(EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory);

            // Find and setup all OnSceneSetup classes in the project.
            IEnumerable<Type> types = ReflectionUtils.GetConcreteImplementationsOf<SceneSetup>();
            IEnumerable<string> setupNames = configuration.GetSetupNames();
            List<SceneSetup> setups = new List<SceneSetup>();

            HashSet<string> initializedKeys = new HashSet<string>();

            foreach (Type onSceneSetupType in types)
            {
                if(setupNames.Contains(onSceneSetupType.FullName) == false)
                {
                    continue;
                }

                try
                {
                    SceneSetup sceneSetup = ReflectionUtils.CreateInstanceOfType(onSceneSetupType) as SceneSetup;

                    if (sceneSetup != null)
                    {
                        setups.Add(sceneSetup);

                        if (sceneSetup.Key != null && initializedKeys.Add(sceneSetup.Key) == false)
                        {
                            Debug.LogWarningFormat("Multiple scene setups with key {0} found during Scene setup. This might cause problems and you might consider using only one.", sceneSetup.Key);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("{0} while initializing SceneSetup object of type {1}.\n{2}", exception.GetType().Name, onSceneSetupType.Name, exception.StackTrace);
                }
            }

            setups = setups.OrderBy(setup => setup.Priority).ToList();

            foreach (SceneSetup onSceneSetup in setups)
            {
                try
                {
                    onSceneSetup.Setup(configuration);
                    Debug.LogFormat("Scene Setup done for {0}", onSceneSetup);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("{0} while initializing SceneSetup object of type {1}.\n{2}", exception.GetType().Name, onSceneSetup.GetType().Name, exception.StackTrace);
                }
            }

            Debug.Log("Scene setup is complete.");
        }
    }
}
