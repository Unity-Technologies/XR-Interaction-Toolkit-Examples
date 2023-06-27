// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core
{
    [Obsolete("This event is not used anymore.")]
    public class ChildDeactivatedEventArgs<TEntity> : EventArgs where TEntity : IEntity
    {
        public TEntity Child { get; private set; }

        public ChildDeactivatedEventArgs(TEntity child)
        {
            Child = child;
        }
    }
}
