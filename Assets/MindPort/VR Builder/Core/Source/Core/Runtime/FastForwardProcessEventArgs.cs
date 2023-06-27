// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core
{
    /// <summary>
    /// EventArgs for fast forward process events.
    /// </summary>
    public class FastForwardProcessEventArgs : ProcessEventArgs
    {
        /// <summary>
        /// Completed transition
        /// </summary>
        public readonly ITransition CompletedTransition;

        public FastForwardProcessEventArgs(ITransition transition, IProcess process) : base(process)
        {
            CompletedTransition = transition;
        }
    }
}
