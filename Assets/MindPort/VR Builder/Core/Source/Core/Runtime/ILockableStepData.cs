// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using VRBuilder.Core.Behaviors;

namespace VRBuilder.Core
{
    /// <summary>
    /// Extends the step data with lockable data.
    /// </summary>
    internal interface ILockableStepData
    {
        /// <summary>
        /// Keeps all the lockable properties referenced which should be unlocked manually.
        /// </summary>
        [DataMember]
        IEnumerable<LockablePropertyReference> ToUnlock { get; set; }

        /// <summary>
        /// Lists all scene object tags to unlock manually.
        /// </summary>
        [DataMember]
        IDictionary<Guid, IEnumerable<Type>> TagsToUnlock { get; set; }
    }
}
