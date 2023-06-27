// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;

namespace VRBuilder.Editor.Configuration
{
    /// <summary>
    /// Interface for editor configuration extension definition.
    /// </summary>
    public interface IEditorConfigurationExtension
    {      
        /// <summary>
        /// Menu items required by this configuration.
        /// </summary>
        IEnumerable<Type> RequiredMenuItems { get; }

        /// <summary>
        /// Menu items disabled by this configuration.
        /// </summary>
        IEnumerable<Type> DisabledMenuItems { get; }
    }
}
