// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Marks a Process drawer as a default drawer for a given type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DefaultProcessDrawerAttribute : Attribute
    {
        /// <summary>
        /// Objects of which type can be processed  by this Process drawer.
        /// </summary>
        public Type DrawableType { get; private set; }

        public DefaultProcessDrawerAttribute(Type type)
        {
            DrawableType = type;
        }
    }
}
