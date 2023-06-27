// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core
{
    /// <summary>
    /// Event that is fired when the current stage changes.
    /// </summary>
    public class ActivationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// New stage.
        /// </summary>
        public readonly Stage Stage;

        public ActivationStateChangedEventArgs(Stage stage)
        {
            Stage = stage;
        }
    }
}
