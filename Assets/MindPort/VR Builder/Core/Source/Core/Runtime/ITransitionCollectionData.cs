// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using VRBuilder.Core.EntityOwners;

namespace VRBuilder.Core
{
    /// <summary>
    /// The interface of a data with a list of <see cref="ITransition"/>s.
    /// </summary>
    public interface ITransitionCollectionData : IEntityCollectionDataWithMode<ITransition>
    {
        /// <summary>
        /// A list of <see cref="ITransition"/>s.
        /// </summary>
        IList<ITransition> Transitions { get; set; }
    }
}
