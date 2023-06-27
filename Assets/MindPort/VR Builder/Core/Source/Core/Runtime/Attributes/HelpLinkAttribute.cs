// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Adds a link to a documentation that explains a behavior or condition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class)]
    public class HelpLinkAttribute : Attribute
    {
        /// <summary>
        /// An HTML link to the documentation explaining the behavior or condition.
        /// </summary>
        public string HelpLink { get; private set; }

        public HelpLinkAttribute(string helpLink)
        {
            HelpLink = helpLink;
        }
    }
}
