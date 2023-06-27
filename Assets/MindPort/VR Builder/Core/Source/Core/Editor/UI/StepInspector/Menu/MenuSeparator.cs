// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor.UI.StepInspector.Menu
{
    /// <summary>
    /// This class adds a separator in the "Add Behavior"/"Add Condition" dropdown menus.
    /// </summary>
    public sealed class MenuSeparator<T> : MenuOption<T>
    {
        /// <summary>
        /// The submenu where separator will be displayed.
        /// </summary>
        public string PathToSubmenu { get; }

        public MenuSeparator(string pathToSubmenu = "")
        {
            PathToSubmenu = pathToSubmenu;
        }
    }
}
