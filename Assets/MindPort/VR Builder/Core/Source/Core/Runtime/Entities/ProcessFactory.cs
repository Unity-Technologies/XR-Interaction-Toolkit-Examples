// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Unity;

namespace VRBuilder.Core
{
    /// <summary>
    /// Factory implementation for <see cref="IProcess"/> objects.
    /// </summary>
    internal class ProcessFactory : Singleton<ProcessFactory>
    {
        /// <summary>
        /// Creates a new <see cref="IProcess"/>.
        /// </summary>
        /// <param name="name"><see cref="IProcess"/>'s name.</param>
        /// <param name="firstStep">Initial <see cref="IStep"/> for this <see cref="IProcess"/>.</param>
        public IProcess Create(string name, IStep firstStep = null)
        {
            return new Process(name, new Chapter("Chapter 1", firstStep));
        }
    }
}
