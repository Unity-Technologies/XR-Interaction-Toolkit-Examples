// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRBuilder.Core.Exceptions;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// Simple ModeHandler.
    /// </summary>
    public sealed class BaseModeHandler : IModeHandler
    {
        /// <inheritdoc />
        public event EventHandler<ModeChangedEventArgs> ModeChanged;

        /// <inheritdoc />
        public int CurrentModeIndex { get; private set; }

        /// <inheritdoc />
        public IMode CurrentMode
        {
            get { return AvailableModes[CurrentModeIndex]; }
        }

        /// <inheritdoc />
        public ReadOnlyCollection<IMode> AvailableModes { get; }

        public BaseModeHandler(List<IMode> modes, int defaultMode = 0)
        {
            AvailableModes = new ReadOnlyCollection<IMode>(modes);
            CurrentModeIndex = defaultMode;
        }

        /// <inheritdoc />
        public void SetMode(int index)
        {
            if (AvailableModes.Count == 0)
            {
                throw new MissingModeException("You cannot access the current process mode index because there are no process modes available.");
            }

            if (CurrentModeIndex >= AvailableModes.Count)
            {
                string message = string.Format("The current process mode index is set to {0} but the current number of available process modes is {1}.", CurrentModeIndex, AvailableModes.Count);
                throw new IndexOutOfRangeException(message);
            }

            CurrentModeIndex = index;

            if (ModeChanged != null)
            {
                ModeChanged(this, new ModeChangedEventArgs(CurrentMode));
            }
        }

        /// <inheritdoc />
        public void SetMode(IMode mode)
        {
            if (AvailableModes.Contains(mode))
            {
                SetMode(AvailableModes.IndexOf(mode));
            }
            else
            {
                throw new MissingModeException("Given mode is not part of the available modes!");
            }
        }
    }
}
