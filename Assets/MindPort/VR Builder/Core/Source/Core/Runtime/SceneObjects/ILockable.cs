// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

﻿using System;

namespace VRBuilder.Core.SceneObjects
{
    /// <summary>
    /// Basic interface for everything which is lockable.
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// Will be called when the object containing this interface is locked.
        /// </summary>
        event EventHandler<LockStateChangedEventArgs> Locked;

        /// <summary>
        /// Will be called when the object containing this interface is unlocked.
        /// </summary>
        event EventHandler<LockStateChangedEventArgs> Unlocked;

        /// <summary>
        /// Returns if the object is locked.
        /// </summary>
        bool IsLocked { get; }

        /// <summary>
        /// Changes the lock state of the object.
        /// </summary>
        void SetLocked(bool lockState);
    }
}
