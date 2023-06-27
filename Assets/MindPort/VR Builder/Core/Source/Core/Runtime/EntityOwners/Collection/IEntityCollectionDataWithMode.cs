// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners
{
    /// <summary>
    /// A generic version of <seealso cref="IEntityCollectionDataWithMode"/>
    /// </summary>
    public interface IEntityCollectionDataWithMode<out TEntity> : IEntityCollectionData<TEntity>, IEntityCollectionDataWithMode where TEntity : IEntity
    {
    }

    /// <summary>
    /// A composition interface of <seealso cref="IEntityCollectionData"/> and <seealso cref="IModeData"/>.
    /// </summary>
    public interface IEntityCollectionDataWithMode : IEntityCollectionData, IModeData
    {
    }
}
