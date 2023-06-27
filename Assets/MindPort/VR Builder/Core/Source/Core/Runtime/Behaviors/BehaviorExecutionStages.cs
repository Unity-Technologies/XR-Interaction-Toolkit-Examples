// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

﻿using System;

namespace VRBuilder.Core.Behaviors
{
    [Flags]
    public enum BehaviorExecutionStages
    {
        Activation = 1 << 0,
        Deactivation = 1 << 1,
        ActivationAndDeactivation = ~0,
        None = 0,
    }
}
