// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners
{
    /// <summary>
    /// An <seealso cref="IEntitySequenceData{TEntity}"/> with <seealso cref="IModeData"/>.
    /// </summary>
    public interface IEntitySequenceDataWithMode<TEntity> : IEntitySequenceData<TEntity>, IModeData where TEntity : IEntity
    {
    }
}
