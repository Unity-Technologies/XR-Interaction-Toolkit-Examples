// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Input
{
    /// <summary>
    /// Can be used to mark a GameObject focusable.
    /// </summary>
    public interface IInputFocus
    {
        /// <summary>
        /// If this is not null the action map with the given name will be set.
        /// </summary>
        string ActionMapName { get; }

        /// <summary>
        /// Return if this object can be focused.
        /// </summary>
        bool CanBeFocused { get; }

        /// <summary>
        /// Will be called when this object is focused.
        /// </summary>
        void OnFocus();

        /// <summary>
        /// Will be called when the object's focus is released.
        /// </summary>
        void OnReleaseFocus();
    }
}
