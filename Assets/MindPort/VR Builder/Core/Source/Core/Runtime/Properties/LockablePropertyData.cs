// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using VRBuilder.Core.Properties;

namespace VRBuilder.Core.RestrictiveEnvironment
{
    /// <summary>
    /// Contains a target <see cref="LockableProperty"/> and additional information which define how the property is handled.
    /// </summary>
    public class LockablePropertyData
    {
        /// <summary>
        /// Target lockable property.
        /// </summary>
        public readonly LockableProperty Property;

        /// <summary>
        /// If true the property is locked in the end of a step.
        /// </summary>
        public bool EndStepLocked = true;

        public LockablePropertyData(LockableProperty property) : this(property, property.EndStepLocked) { }

        public LockablePropertyData(LockableProperty property, bool endStepLocked)
        {
            EndStepLocked = endStepLocked;
            Property = property;
        }

        protected bool Equals(LockablePropertyData other)
        {
            return Equals(Property, other.Property);
        }

        ///  <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LockablePropertyData) obj);
        }

        ///  <inheritdoc/>
        public override int GetHashCode()
        {
            return (Property != null ? Property.GetHashCode() : 0);
        }
    }
}
