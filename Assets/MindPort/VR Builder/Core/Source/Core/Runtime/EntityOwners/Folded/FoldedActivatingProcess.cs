// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;
using System.Collections.Generic;

namespace VRBuilder.Core.EntityOwners.FoldedEntityCollection
{
    /// <summary>
    /// An activating process over an entities' sequence which activates all entities in order.
    /// </summary>
    internal class FoldedActivatingProcess<TEntity> : StageProcess<IEntitySequenceDataWithMode<TEntity>> where TEntity : IEntity
    {
        private IEnumerator<TEntity> enumerator;

        public FoldedActivatingProcess(IEntitySequenceDataWithMode<TEntity> data) : base(data)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
            enumerator = Data.GetChildren().GetEnumerator();
            enumerator.Reset();
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            while (enumerator.MoveNext())
            {
                Data.Current = enumerator.Current;

                if (Data.Current == null)
                {
                    continue;
                }

                Data.Current.LifeCycle.Activate();

                if (Data.Current.LifeCycle.Stage == Stage.Activating && Data.Mode.CheckIfSkipped(Data.Current.GetType()))
                {
                    Data.Current.LifeCycle.MarkToFastForwardStage(Stage.Activating);
                }

                while (Data.Current.LifeCycle.Stage != Stage.Active)
                {
                    yield return null;
                }
            }
        }

        /// <inheritdoc />
        public override void End()
        {
            enumerator = null;
        }

        /// <inheritdoc />
        public override void FastForward()
        {
            if (Equals(Data.Current, default(IEntity)))
            {
                if (enumerator.MoveNext())
                {
                    Data.Current = enumerator.Current;
                }
            }

            while (Equals(Data.Current, default(IEntity)) == false)
            {
                if (Data.Current.LifeCycle.Stage == Stage.Inactive)
                {
                    Data.Current.LifeCycle.Activate();
                }

                if (Data.Current.LifeCycle.Stage == Stage.Activating)
                {
                    Data.Current.LifeCycle.MarkToFastForwardStage(Stage.Activating);
                }

                Data.Current = enumerator.MoveNext() ? enumerator.Current : default;
            }
        }
    }
}
