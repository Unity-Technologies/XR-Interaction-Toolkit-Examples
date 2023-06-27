// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Linq;

namespace VRBuilder.Core.EntityOwners.FoldedEntityCollection
{
    /// <summary>
    /// An active process over a sequence of entities.
    /// </summary>
    internal class FoldedActiveProcess<TEntity> : StageProcess<IEntitySequenceDataWithMode<TEntity>> where TEntity : IEntity
    {
        public FoldedActiveProcess(IEntitySequenceDataWithMode<TEntity> data) : base(data)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            foreach (TEntity child in Data.GetChildren()
                .Where(child => child.LifeCycle.Stage == Stage.Active)
                .Where(child => Data.Mode.CheckIfSkipped(child.GetType())))
            {
                child.LifeCycle.MarkToFastForwardStage(Stage.Active);
            }

            yield break;
        }

        /// <inheritdoc />
        public override void End()
        {
        }

        /// <inheritdoc />
        public override void FastForward()
        {
            foreach (TEntity child in Data.GetChildren())
            {
                if (child.LifeCycle.Stage == Stage.Active)
                {
                    child.LifeCycle.MarkToFastForwardStage(Stage.Active);
                }
            }
        }
    }
}
