// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace VRBuilder.Core
{
    /// <summary>
    /// Implementation of <see cref="IMetadata"/> adapted for <see cref="IChapter"/> data.
    /// </summary>
    [DataContract(IsReference = true)]
    public class ChapterMetadata : IMetadata
    {
        /// <summary>
        /// Reference to last selected <see cref="IStep"/>.
        /// </summary>
        [DataMember]
        public IStep LastSelectedStep { get; set; }

        /// <summary>
        /// Reference to the entry node's position in the Workflow window.
        /// </summary>
        [DataMember]
        public Vector2 EntryNodePosition { get; set; }

        /// <summary>
        /// Unique identifier for chapter.
        /// </summary>
        [DataMember]
        public Guid Guid { get; set; }

        public ChapterMetadata()
        {
        }
    }
}
