// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Unity;

namespace VRBuilder.Core
{
    /// <summary>
    /// Factory implementation for <see cref="ITransition"/> objects.
    /// </summary>
    internal class TransitionFactory : Singleton<TransitionFactory>
    {
        /// <summary>
        /// Creates a new <see cref="ITransition"/>.
        /// </summary>
        public ITransition Create()
        {
            return new Transition();
        }
    }
}
