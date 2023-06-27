// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.Utils;
using UnityEngine;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Configurator to set the process runtime configuration which is used by a process during its execution.
    /// There has to be one and only one process runtime configurator game object per scene.
    /// </summary>
    public sealed class RuntimeConfigurator : MonoBehaviour
    {
        /// <summary>
        /// The event that fires when a process mode or runtime configuration changes.
        /// </summary>
        public static event EventHandler<ModeChangedEventArgs> ModeChanged;

        /// <summary>
        /// The event that fires when a process runtime configuration changes.
        /// </summary>
        public static event EventHandler<EventArgs> RuntimeConfigurationChanged;

        /// <summary>
        /// Fully qualified name of the runtime configuration used.
        /// This field is magically filled by <see cref="RuntimeConfiguratorEditor"/>
        /// </summary>
        [SerializeField]
        private string runtimeConfigurationName = typeof(DefaultRuntimeConfiguration).AssemblyQualifiedName;

        /// <summary>
        /// Process name which is selected.
        /// This field is magically filled by <see cref="RuntimeConfiguratorEditor"/>
        /// </summary>
        [SerializeField]
        private string selectedProcessStreamingAssetsPath = "";

        private BaseRuntimeConfiguration runtimeConfiguration;

        private static RuntimeConfigurator instance;

        private static RuntimeConfigurator LookUpForGameObject()
        {
            RuntimeConfigurator[] instances = FindObjectsOfType<RuntimeConfigurator>();

            if (instances.Length > 1)
            {
                Debug.LogError("More than one process runtime configurator is found in the scene. Taking the first one. This may lead to unexpected behaviour.");
            }

            if (instances.Length == 0)
            {
                return null;
            }

            return instances[0];
        }

        /// <summary>
        /// Checks if a process runtime configurator instance exists in scene.
        /// </summary>
        public static bool Exists
        {
            get
            {
                if (instance == null || instance.Equals(null))
                {
                    instance = LookUpForGameObject();
                }

                return (instance != null && instance.Equals(null) == false);
            }
        }

        /// <summary>
        /// Shortcut to get the <see cref="IRuntimeConfiguration"/> of the instance.
        /// </summary>
        public static BaseRuntimeConfiguration Configuration
        {
            get
            {
                if (Instance.runtimeConfiguration != null)
                {
                    return Instance.runtimeConfiguration;
                }

                Type type = ReflectionUtils.GetTypeFromAssemblyQualifiedName(Instance.runtimeConfigurationName);

                if (type == null)
                {
                    Debug.LogErrorFormat("IRuntimeConfiguration type '{0}' cannot be found. Using '{1}' instead.", Instance.runtimeConfigurationName, typeof(DefaultRuntimeConfiguration).AssemblyQualifiedName);
                    type = typeof(DefaultRuntimeConfiguration);
                }
#pragma warning disable 0618
                IRuntimeConfiguration config = (IRuntimeConfiguration)ReflectionUtils.CreateInstanceOfType(type);
                if (config is BaseRuntimeConfiguration configuration)
                {
                    Configuration = configuration;
                }
                else
                {
                    Debug.LogWarning("Your runtime configuration only extends the interface IRuntimeConfiguration, please consider moving to BaseRuntimeConfiguration as base class.");
                    Configuration = new RuntimeConfigWrapper(config);
                }
#pragma warning restore 0618
                return Instance.runtimeConfiguration;
            }
            set
            {
                if (value == null)
                {
                    Debug.LogError("Process runtime configuration cannot be null.");
                    return;
                }

                if (Instance.runtimeConfiguration == value)
                {
                    return;
                }

                if (Instance.runtimeConfiguration != null)
                {
                    Instance.runtimeConfiguration.Modes.ModeChanged -= RuntimeConfigurationModeChanged;
                }

                value.Modes.ModeChanged += RuntimeConfigurationModeChanged;

                Instance.runtimeConfigurationName = value.GetType().AssemblyQualifiedName;
                Instance.runtimeConfiguration = value;

                EmitRuntimeConfigurationChanged();
            }
        }

        /// <summary>
        /// Current instance of the RuntimeConfigurator.
        /// </summary>
        /// <exception cref="NullReferenceException">Will throw a NPE if there is no RuntimeConfigurator added to the scene.</exception>
        public static RuntimeConfigurator Instance
        {
            get
            {
                if (Exists == false)
                {
                    throw new NullReferenceException("Process runtime configurator is not set in the scene. Create an empty game object with the 'RuntimeConfigurator' script attached to it.");
                }

                return instance;
            }
        }

        /// <summary>
        /// Returns the assembly qualified name of the runtime configuration.
        /// </summary>
        public string GetRuntimeConfigurationName()
        {
            return runtimeConfigurationName;
        }

        /// <summary>
        /// Sets the runtime configuration name, expects an assembly qualified name.
        /// </summary>
        public void SetRuntimeConfigurationName(string configurationName)
        {
            runtimeConfigurationName = configurationName;
        }

        /// <summary>
        /// Returns the path to the selected process.
        /// </summary>
        public string GetSelectedProcess()
        {
            return selectedProcessStreamingAssetsPath;
        }

        /// <summary>
        /// Sets the path to the selected process.
        /// </summary>
        public void SetSelectedProcess(string path)
        {
            selectedProcessStreamingAssetsPath = path;
        }

        private void Awake()
        {
            Configuration.SceneObjectRegistry.RegisterAll();
            RuntimeConfigurationChanged += HandleRuntimeConfigurationChanged;
        }

        private void OnDestroy()
        {
            ModeChanged = null;
            RuntimeConfigurationChanged = null;
        }

        private static void EmitModeChanged()
        {
            ModeChanged?.Invoke(Instance, new ModeChangedEventArgs(Instance.runtimeConfiguration.Modes.CurrentMode));
        }

        private static void EmitRuntimeConfigurationChanged()
        {
            RuntimeConfigurationChanged?.Invoke(Instance, EventArgs.Empty);
        }

        private void HandleRuntimeConfigurationChanged(object sender, EventArgs args)
        {
            EmitModeChanged();
        }

        private static void RuntimeConfigurationModeChanged(object sender, ModeChangedEventArgs modeChangedEventArgs)
        {
            EmitModeChanged();
        }
    }
}
