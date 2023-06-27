// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.EntityOwners
{
    public interface IEntitySequenceData<TEntity> : IEntityCollectionData<TEntity> where TEntity : IEntity
    {
        /// <summary>
        /// Current entity in the sequence.
        /// </summary>
        TEntity Current { get; set; }
    }
}
