// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Linq;
using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners.ParallelEntityCollection
{
    /// <summary>
    /// A process which deactivates a collection of entities in parallel.
    /// </summary>
    internal class ParallelDeactivatingProcess<TCollectionData> : Process<TCollectionData> where TCollectionData : class, IEntityCollectionData, IModeData
    {
        public ParallelDeactivatingProcess(TCollectionData data) : base(data)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
            foreach (IEntity child in Data.GetChildren().Where(child => Data.Mode.CheckIfSkipped(child.GetType()) == false))
            {
                child.LifeCycle.Deactivate();
            }
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            while (GetBlockingChildren(Data, Data.Mode).Any(child => child.LifeCycle.Stage == Stage.Deactivating))
            {
                yield return null;
            }
        }

        /// <inheritdoc />
        public override void End()
        {
            foreach (IEntity child in Data.GetChildren().Where(child => child.LifeCycle.Stage != Stage.Inactive))
            {
                child.LifeCycle.MarkToFastForward();
            }
        }

        /// <inheritdoc />
        public override void FastForward()
        {
        }
    }
}
