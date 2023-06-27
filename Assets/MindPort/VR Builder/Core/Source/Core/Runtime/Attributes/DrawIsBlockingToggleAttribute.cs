// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Reflection;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Declares that the "Is Blocking" toggle has to be drawn, if the behavior implements <see cref="IBackgroundBehaviorData"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DrawIsBlockingToggleAttribute : MetadataAttribute
    {
        /// <inheritdoc />
        public override object GetDefaultMetadata(MemberInfo owner)
        {
            return null;
        }

        /// <inheritdoc />
        public override bool IsMetadataValid(object metadata)
        {
            return metadata == null;
        }
    }
}
