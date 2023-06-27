// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Behaviors;

namespace VRBuilder.Core
{
    /// <summary>
    /// Interface that enables non-blocking background behaviors.
    /// If the `IsBlocking` property returns false, the behavior will not hinder the completion of a step.
    /// </summary>
    public interface IBackgroundBehaviorData : IBehaviorData
    {
        /// <summary>
        /// If true, the behavior prevents the completion of a step until it is completed itself.
        /// If false, the behavior does not hinder the completion of a step.
        /// </summary>
        [DataMember]
        [HideInProcessInspector]
        bool IsBlocking { get; set; }
    }
}
