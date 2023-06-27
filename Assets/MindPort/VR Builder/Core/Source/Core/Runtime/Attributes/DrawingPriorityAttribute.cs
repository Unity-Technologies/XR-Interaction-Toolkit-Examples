// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Declares the drawing order for this element.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DrawingPriorityAttribute : Attribute
    {
        /// <summary>
        /// Lower goes first.
        /// </summary>
        public int Priority { get; private set; }

        public DrawingPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}
