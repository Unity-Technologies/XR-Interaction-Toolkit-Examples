// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;

namespace VRBuilder.Editor.Tabs
{
    /// <summary>
    /// Draws a view with multiple tabs.
    /// </summary>
    internal interface ITabsGroup
    {
        /// <summary>
        /// Index of the currently selected tab.
        /// </summary>
        int Selected { get; set; }

        /// <summary>
        /// Tabs to display. See <seealso cref="ITab"/>.
        /// </summary>
        IList<ITab> Tabs { get; }
    }
}
