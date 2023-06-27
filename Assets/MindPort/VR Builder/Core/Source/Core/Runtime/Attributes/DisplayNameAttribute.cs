// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Displayed name of process entity's property or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// Name of the process entity's property or field.
        /// </summary>
        public string Name { get; private set; }

        public DisplayNameAttribute(string name)
        {
            Name = name;
        }
    }
}
