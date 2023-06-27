// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;

namespace VRBuilder.Core
{
    /// <summary>
    /// Abstract holder of data.
    /// </summary>
    public interface IDataOwner
    {
        /// <summary>
        /// Abstract data.
        /// </summary>
        IData Data { get; }
    }

    /// <summary>
    /// Abstract holder of data.
    /// </summary>
    public interface IDataOwner<out TData> : IDataOwner
    {
        /// <summary>
        /// Abstract data.
        /// </summary>
        [DataMember]
        new TData Data { get; }
    }
}
