// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Unity;

namespace VRBuilder.Core
{
    /// <summary>
    /// Factory implementation for <see cref="IStep"/> objects.
    /// </summary>
    internal class StepFactory : Singleton<StepFactory>
    {
        /// <summary>
        /// Creates a new <see cref="IStep"/>.
        /// </summary>
        /// <param name="name"><see cref="IStep"/>'s name.</param>
        public IStep Create(string name)
        {
            return new Step(name);
        }
    }
}
