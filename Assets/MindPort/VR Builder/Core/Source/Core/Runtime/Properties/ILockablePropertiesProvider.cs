// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using VRBuilder.Core.RestrictiveEnvironment;

namespace VRBuilder.Core
{
    /// <summary>
    /// This interface is used to allow entities, for example <see cref="Transition"/> or <see cref="Conditions"/>
    /// to provide properties which should be locked.
    /// </summary>
    public interface ILockablePropertiesProvider
    {
        /// <summary>
        /// Returns all LockableProperties this provider requires.
        /// </summary>
        IEnumerable<LockablePropertyData> GetLockableProperties();
    }
}
