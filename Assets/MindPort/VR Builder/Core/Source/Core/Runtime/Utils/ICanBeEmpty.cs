// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Runtime.Properties
{
    /// <summary>
    /// Interface which allows validation to check if the object validated is empty.
    /// </summary>
    public interface ICanBeEmpty
    {
        /// <summary>
        /// Returns true when this object is not properly filled.
        /// </summary>
        bool IsEmpty();
    }
}
