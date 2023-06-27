// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;

namespace VRBuilder.Core
{
    /// <summary>
    /// A base implementation of a <seealso cref="IStageProcess"/> which provides access to its entity's data.
    /// </summary>
    public abstract class StageProcess<TData> : IStageProcess where TData : class, IData
    {
        /// <summary>
        /// The entity's data.
        /// </summary>
        protected TData Data { get; }

        /// <summary>
        /// The entity owning the data.
        /// </summary>
        protected IEntity Outer { get; }

        protected StageProcess(TData data, IEntity outer = null)
        {
            Data = data;
            Outer = outer;
        }

        /// <inheritdoc />
        public abstract void Start();

        /// <inheritdoc />
        public abstract IEnumerator Update();

        /// <inheritdoc />
        public abstract void End();

        /// <inheritdoc />
        public abstract void FastForward();
    }
}
