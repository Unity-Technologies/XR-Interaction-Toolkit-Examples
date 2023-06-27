// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;
using System;
using System.Collections.Generic;
using VRBuilder.Core.Configuration.Modes;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using System.Linq;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Process runtime configuration which is used if no other was implemented.
    /// </summary>
    public class DefaultRuntimeConfiguration : BaseRuntimeConfiguration
    {
        private IProcessAudioPlayer processAudioPlayer;
        private ISceneObjectManager sceneObjectManager;

        /// <summary>
        /// Default mode which white lists everything.
        /// </summary>
        public static readonly IMode DefaultMode = new Mode("Default", new WhitelistTypeRule<IOptional>());

        public DefaultRuntimeConfiguration()
        {
            Modes = new BaseModeHandler(new List<IMode> {DefaultMode});
        }

        /// <inheritdoc />
        public override ProcessSceneObject User
        {
            get
            {
                ProcessSceneObject user = Users.FirstOrDefault();

                if (user == null)
                {
                    throw new Exception("Could not find a UserSceneObject in the scene.");
                }

                return user;
            }
        }

        /// <inheritdoc />
        public override AudioSource InstructionPlayer
        {
            get
            {
                return ProcessAudioPlayer.FallbackAudioSource;
            }
        }

        /// <inheritdoc />
        public override IProcessAudioPlayer ProcessAudioPlayer
        {
            get
            {
                if (processAudioPlayer == null)
                {
                    processAudioPlayer = new DefaultAudioPlayer();
                }

                return processAudioPlayer;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<UserSceneObject> Users => GameObject.FindObjectsOfType<UserSceneObject>();

        /// <inheritdoc />
        public override ISceneObjectManager SceneObjectManager
        {
            get
            {
                if (sceneObjectManager == null)
                {
                    sceneObjectManager = new DefaultSceneObjectManager();
                }

                return sceneObjectManager;
            }
        }
    }
}
