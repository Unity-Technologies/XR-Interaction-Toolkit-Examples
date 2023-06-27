// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using UnityEngine;

namespace VRBuilder.Core.Audio
{
    /// <summary>
    /// Unity resource based audio data.
    /// </summary>
    [DataContract(IsReference = true)]
    [DisplayName("Play Audio File")]
    public class ResourceAudio : IAudioData
    {
        private string path;

        /// <summary>
        /// File path relative to the Resources folder.
        /// </summary>
        [DataMember]
        public string ResourcesPath
        {
            get
            {
                return path;
            }
            set
            {
                path = value;
                if (Application.isPlaying)
                {
                    InitializeAudioClip();
                }
            }
        }

        public ResourceAudio(string path)
        {
            ResourcesPath = path;
        }

        protected ResourceAudio()
        {
            path = "";
        }

        public bool HasAudioClip
        {
            get
            {
                return AudioClip != null;
            }
        }

        /// <inheritdoc/>
        public AudioClip AudioClip { get; private set; }

        /// <inheritdoc/>
        public string ClipData
        {
            get
            {
                return ResourcesPath;
            }
            set
            {
                ResourcesPath = value;
            }
        }

        public void InitializeAudioClip()
        {
            AudioClip = null;

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarningFormat("Path to audio file is not defined.");
                return;
            }

            AudioClip = Resources.Load<AudioClip>(path);

            if (HasAudioClip == false)
            {
                Debug.LogWarningFormat("Given path '{0}' to resource has returned no audio clip", path);
            }
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(ResourcesPath);
        }
    }
}
