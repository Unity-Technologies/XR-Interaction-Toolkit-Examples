// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.EntityOwners;

namespace VRBuilder.Core
{
    /// <summary>
    /// The interface for a step's data.
    /// </summary>
    public interface IStepData : IRenameableData, IDescribedData, IEntitySequenceDataWithMode<IStepChild>
    {
        /// <summary>
        /// The list of the step's behaviors.
        /// </summary>
        IBehaviorCollection Behaviors { get; set; }

        /// <summary>
        /// The list of the step's transitions.
        /// </summary>
        ITransitionCollection Transitions { get; set; }
    }
}
