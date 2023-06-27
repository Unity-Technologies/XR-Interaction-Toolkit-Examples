// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections;

namespace VRBuilder.Core
{
    /// <summary>
    /// A convenience class for processes that happen instantly. You only have to implement the <see cref="Start"/> method.
    /// </summary>
    public abstract class InstantProcess<TData> : StageProcess<TData> where TData : class, IData
    {
        protected InstantProcess(TData data) : base(data)
        {
        }

        ///<inheritdoc />
        public abstract override void Start();

        ///<inheritdoc />
        public override IEnumerator Update()
        {
            yield break;
        }

        ///<inheritdoc />
        public override void End()
        {
        }

        ///<inheritdoc />
        public override void FastForward()
        {
        }
    }
}
