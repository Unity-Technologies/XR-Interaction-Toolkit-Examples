// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// This is a <see cref="EventArgs"/> used for <see cref="IMode"/> changes.
    /// If you want so see more about EventArgs, please visit: https://docs.microsoft.com/en-us/dotnet/standard/events/
    /// </summary>
    public class ModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The newly activated <see cref="IMode"/>.
        /// </summary>
        public IMode Mode { get; private set; }

        public ModeChangedEventArgs(IMode mode)
        {
            Mode = mode;
        }
    }
}
