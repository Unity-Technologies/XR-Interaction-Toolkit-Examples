// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Serialization
{
    /// <summary>
    /// Every class with this attribute which also extends JsonConverter will be added as converter to the <see cref="NewtonsoftJsonProcessSerializer"/>.
    /// </summary>
    public class NewtonsoftConverterAttribute : Attribute
    {
        /// <summary>
        /// Defines the order in which converters should be applied, the lower value triggers earlier.
        /// </summary>
        public int Priority { get; }

        /// <param name="priority">Set the value of <see cref="Priority"/> property.</param>
        public NewtonsoftConverterAttribute(int priority = 1024)
        {
            Priority = priority;
        }
    }
}
