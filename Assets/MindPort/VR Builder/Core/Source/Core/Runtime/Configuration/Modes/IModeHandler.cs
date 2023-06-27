// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.ObjectModel;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// Interface for the ModeHandler used to configure modes during runtime.
    /// </summary>
    public interface IModeHandler
    {
        /// <summary>
        /// The event that has to be invoked when the current mode changes, for example in <see cref="SetMode"/> method.
        /// </summary>
        event EventHandler<ModeChangedEventArgs> ModeChanged;

        /// <summary>
        /// The ordered collection of all available process modes.
        /// </summary>
        ReadOnlyCollection<IMode> AvailableModes { get; }

        /// <summary>
        /// The index of the current process mode.
        /// </summary>
        int CurrentModeIndex { get; }

        /// <summary>
        /// The current process mode.
        /// </summary>
        IMode CurrentMode { get; }

        /// <summary>
        /// Set the current process mode.
        /// </summary>
        /// <param name="index">The index of the desired process mode.</param>
        void SetMode(int index);

        /// <summary>
        /// Set the current process mode, this process mode has to be one of the available modes.
        /// </summary>
        /// <param name="mode">The desired process mode which should be set.</param>
        void SetMode(IMode mode);
    }
}
