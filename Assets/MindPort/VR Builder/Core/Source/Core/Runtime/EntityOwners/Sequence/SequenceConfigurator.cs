// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Configuration.Modes;

namespace VRBuilder.Core.EntityOwners
{
    /// <summary>
    /// A configurator for a sequence of entities.
    /// </summary>
    public class SequenceConfigurator<TEntity> : Configurator<IEntitySequenceData<TEntity>> where TEntity : IEntity
    {
        public SequenceConfigurator(IEntitySequenceData<TEntity> data) : base(data)
        {
        }

        ///<inheritdoc />
        public override void Configure(IMode mode, Stage stage)
        {
            if (Data.Current == null)
            {
                return;
            }

            if (Data.Current is IOptional
                && mode.CheckIfSkipped(Data.Current.GetType())
                && Data.Current.LifeCycle.Stage != Stage.Inactive)
            {
                Data.Current.LifeCycle.MarkToFastForward();
            }
        }
    }
}
