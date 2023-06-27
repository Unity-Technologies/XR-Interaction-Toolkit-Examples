// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Serialization;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// An interface for process runtime configurations. Implement it to create your own.
    /// </summary>
    [Obsolete("To be more flexible with development we switched to an abstract class as configuration base, consider using BaseRuntimeConfiguration.")]
    public interface IRuntimeConfiguration
    {
        /// <summary>
        /// SceneObjectRegistry gathers all created ProcessSceneEntities.
        /// </summary>
        ISceneObjectRegistry SceneObjectRegistry { get; }

        /// <summary>
        /// Defines the serializer which should be used to serialize processes.
        /// </summary>
        IProcessSerializer Serializer { get; set; }

        /// <summary>
        /// Returns the mode handler for the process.
        /// </summary>
        IModeHandler Modes { get; }

        /// <summary>
        /// User scene object.
        /// </summary>
        [Obsolete("Use Users instead.")]
        ProcessSceneObject User { get; }

        /// <summary>
        /// All user scene objects in the scene.
        /// </summary>
        IEnumerable<UserSceneObject> Users { get; }

        /// <summary>
        /// Default audio source to play audio from.
        /// </summary>
        [Obsolete("Use ProcessAudioPlayer instead")]
        AudioSource InstructionPlayer { get; }

        /// <summary>
        /// Default player for process-originated audio.
        /// </summary>
        IProcessAudioPlayer ProcessAudioPlayer { get; }

        /// <summary>
        /// Object that handles scene objects operations.
        /// </summary>
        ISceneObjectManager SceneObjectManager { get; }

        /// <summary>
        /// Object that stores configuration specific to the scene.
        /// </summary>
        ISceneConfiguration SceneConfiguration { get; }

        /// <summary>
        /// Synchronously returns the deserialized process from given path.
        /// </summary>
        Task<IProcess> LoadProcess(string path);
    }
}
