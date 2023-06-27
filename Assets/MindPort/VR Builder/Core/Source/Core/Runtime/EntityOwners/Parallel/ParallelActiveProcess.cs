// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Linq;
using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners.ParallelEntityCollection
{
    /// <summary>
    /// A process for a collection of entities which are activated and deactivated in parallel.
    /// </summary>
    internal class ParallelActiveProcess<TCollectionData> : Process<TCollectionData> where TCollectionData : class, IEntityCollectionData, IModeData
    {
        public ParallelActiveProcess(TCollectionData data) : base(data)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            int endlessIterationCheck = 0;
            while (endlessIterationCheck < 1000000)
            {
                yield return null;
                endlessIterationCheck++;
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
