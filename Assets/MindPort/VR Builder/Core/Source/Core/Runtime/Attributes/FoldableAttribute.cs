// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Reflection;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Declares that this element's view has to be foldable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FoldableAttribute : MetadataAttribute
    {
        /// <inheritdoc />
        public override object GetDefaultMetadata(MemberInfo owner)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool IsMetadataValid(object metadata)
        {
            return metadata is bool;
        }
    }
}
