// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.SceneObjects
{
    public class LockStateChangedEventArgs : EventArgs
    {
        public readonly bool IsLocked;

        public LockStateChangedEventArgs(bool isLocked)
        {
            IsLocked = isLocked;
        }
    }
}
