// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Input
{
    /// <summary>
    /// Allows to prioritize input actions.
    /// </summary>
    public interface IInputActionListener
    {
        /// <summary>
        /// Priority of this input.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// If this listener ignores a set focus, it will also be called when focus is active.
        /// </summary>
        bool IgnoreFocus { get; }
    }
}
