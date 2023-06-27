// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core
{
    /// <summary>
    /// A collection of <see cref="Behaviors.IBehavior"/>s of a <see cref="IStep"/>.
    /// </summary>
    public interface IBehaviorCollection : IStepChild, IDataOwner<IBehaviorCollectionData>, IClonable<IBehaviorCollection>
    {
    }
}
