// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners.ParallelEntityCollection
{
    /// <summary>
    /// A base process for entity collection.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    internal abstract class Process<TData> : Core.StageProcess<TData> where TData : class, IEntityCollectionData, IModeData
    {
        /// <summary>
        /// Takes a <paramref name="collection"/> of entities and filters out the ones that must be skipped due to <paramref name="mode"/>
        /// or contains a <seealso cref="IBackgroundBehaviorData"/> with `IsBlocking` set to false.
        /// </summary>
        protected IEnumerable<IEntity> GetBlockingChildren(IEntityCollectionData collection, IMode mode)
        {
            return collection.GetChildren()
                .Where(child => mode.CheckIfSkipped(child.GetType()) == false)
                .Where(child =>
                {
                    IDataOwner dataOwner = child as IDataOwner;
                    if (dataOwner == null)
                    {
                        return true;
                    }

                    IBackgroundBehaviorData blockingData = dataOwner.Data as IBackgroundBehaviorData;
                    return blockingData == null || blockingData.IsBlocking;
                });
        }

        protected Process(TData data) : base(data)
        {
        }
    }
}
