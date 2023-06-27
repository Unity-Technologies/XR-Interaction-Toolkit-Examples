// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core
{
    /// <summary>
    /// Base interface for objects which can be completed.
    /// </summary>
    public interface ICompletable
    {
        /// <summary>
        /// True if this instance is already completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Forces this instance's completion.
        /// </summary>
        void Autocomplete();
    }
}
