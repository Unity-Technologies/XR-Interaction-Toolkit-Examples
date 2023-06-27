// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;

namespace VRBuilder.Core.EntityOwners
{
    /// <summary>
    /// A base class for data classes that are collections of other entities.
    /// </summary>
    public abstract class EntityCollectionData<TEntity> : IEntityCollectionData<TEntity> where TEntity : IEntity
    {
        /// <inheritdoc />
        public Metadata Metadata { get; set; }

        /// <inheritdoc />
        public abstract IEnumerable<TEntity> GetChildren();

        /// <inheritdoc />
        IEnumerable<IEntity> IEntityCollectionData.GetChildren()
        {
            return GetChildren().Cast<IEntity>();
        }
    }
}
