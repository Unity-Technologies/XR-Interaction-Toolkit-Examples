// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;

namespace VRBuilder.Core.Conditions
{
    /// <summary>
    /// An abstract class for processes for Active <see cref="Stage"/> of <see cref="ICompletableEntity"/>.
    /// </summary>
    public abstract class BaseActiveProcessOverCompletable<TData> : StageProcess<TData> where TData : class, ICompletableData
    {
        protected BaseActiveProcessOverCompletable(TData data, IEntity outer = null) : base(data, outer)
        {
        }

        /// <inheritdoc />
        public override void Start()
        {
            Data.IsCompleted = false;
        }

        /// <inheritdoc />
        public override IEnumerator Update()
        {
            while (CheckIfCompleted() == false)
            {
                yield return null;
            }

            Data.IsCompleted = true;
        }

        /// <inheritdoc />
        public override void End()
        {
        }

        /// <inheritdoc />
        public override void FastForward()
        {
        }

        /// <summary>
        /// Implement your custom check in this method. The process will not complete until this method returns true.
        /// </summary>
        protected abstract bool CheckIfCompleted();
    }
}
