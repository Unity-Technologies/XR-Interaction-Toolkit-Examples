// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core
{
    /// <summary>
    /// Data structure with an <see cref="IStep"/>'s description.
    /// </summary>
    public interface IDescribedData : IData
    {
        /// <summary>
        /// <see cref="IStep"/>'s description.
        /// </summary>
        string Description { get; set; }
    }
}
