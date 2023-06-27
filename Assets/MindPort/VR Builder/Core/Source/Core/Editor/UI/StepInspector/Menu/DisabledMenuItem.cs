// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Editor.UI.StepInspector.Menu
{
    /// <summary>
    /// The Step Inspector populates "Add Behavior" and "Add Condition" buttons' dropdown menus with implementations of this class.
    /// Use either <seealso cref="IBehavior"/> or <see cref="ICondition"/> as the generic parameter.
    /// The Step Inspector will display it as a disabled option with <see cref="get_DisplayedName"/>.
    /// </summary>
    public class DisabledMenuItem<T> : MenuOption<T>
    {
        /// <summary>
        /// A name displayed in the Step Inspector.
        /// </summary>
        public string Label { get; }

        /// <param name="label">The displayed text.</param>
        public DisabledMenuItem(string label)
        {
            Label = label;
        }
    }
}
