// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core;

namespace VRBuilder.Editor.ProcessValidation
{
    /// <summary>
    /// Retrieves <see cref="IContext"/> from any provided <see cref="IData"/>.
    /// </summary>
    public interface IContextResolver
    {
        /// <summary>
        /// Resolves the fitting <see cref="IContext"/> for the given <see cref="IData"/>.
        /// </summary>
        IContext FindContext(IData data, IProcess process);
    }
}
