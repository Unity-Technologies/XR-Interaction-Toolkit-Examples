// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Reflection;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Base class for metadata attributes which define special rules for drawing the element.
    /// </summary>
    public abstract class MetadataAttribute : Attribute
    {
        /// <summary>
        /// Name of attribute.
        /// </summary>
        public string Name
        {
            get
            {
                return GetType().FullName;
            }
        }

        /// <summary>
        /// Return default metadata for <paramref name="owner"/>.
        /// </summary>
        public abstract object GetDefaultMetadata(MemberInfo owner);

        /// <summary>
        /// Check if <paramref name="metadata"/> is still valid.
        /// </summary>
        public abstract bool IsMetadataValid(object metadata);
    }
}
