// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Runtime.Serialization;

namespace VRBuilder.Core
{
    /// <summary>
    /// Abstract data structure.
    /// </summary>
    public interface IData
    {
        /// <summary>
        /// Reference to this object's <see cref="IMetadata"/>.
        /// </summary>
        [DataMember]
        Metadata Metadata { get; set; }
    }
}
