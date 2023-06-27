// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Declares that children of this list have metadata attributes and can be reordered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReorderableListOfAttribute : ListOfAttribute
    {
        /// <inheritdoc />
        public ReorderableListOfAttribute(params Type[] childAttributes) : base(childAttributes) { }
    }
}
