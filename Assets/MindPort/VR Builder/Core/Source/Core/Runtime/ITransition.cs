// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core
{
    /// <summary>
    /// An interface for a transition that determines when a <see cref="IStep"/> is completed and what is the next <see cref="IStep"/>.
    /// </summary>
    public interface ITransition : IEntity, ICompletable, IDataOwner<ITransitionData>, IClonable<ITransition>
    {
    }
}
