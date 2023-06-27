// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor.ProcessValidation
{
    /// <summary>
    /// Context is used to indicate the position in the process structure.
    /// </summary>
    public interface IContext
    {
        /// <summary>
        /// Parent context, can be null.
        /// </summary>
        IContext Parent { get; }

        /// <summary>
        /// Produces a readable string which allows us to find the context in editor.
        /// </summary>
        string ToString();
    }
}
