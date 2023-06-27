// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Linq;
using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners.ParallelEntityCollection
{
    /// <summary>
    /// A process over a collection of entities which activates them at the same time, in parallel.
    /// </summary>
    internal class ParallelActivatingProcess<TCollectionData> : Process<TCollectionData> where TCollectionData : class, IEntityCollectionData, IModeData
    {
        public ParallelActivatingProcess(TCollectionData data) : base(data)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
            foreach (IEntity child in Data.GetChildren().Where(child => Data.Mode.CheckIfSkipped(child.GetType()) == false))
            {
                child.LifeCycle.Activate();
            }
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            while (GetBlockingChildren(Data, Data.Mode).Any(child => child.LifeCycle.Stage == Stage.Activating))
            {
                yield return null;
            }
        }

        /// <inheritdoc />
        public override void End()
        {
        }

        /// <inheritdoc />
        public override void FastForward()
        {
            foreach (IEntity child in Data.GetChildren().Where(child => child.LifeCycle.Stage == Stage.Activating))
            {
                child.LifeCycle.MarkToFastForwardStage(Stage.Activating);
            }
        }
    }
}
