// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Attributes
{
    /// <summary>
    /// Use this attribute to explicitly specify an implementation of `IProcessDrawer` that should be used.
    /// The drawer type is passed as string because you can't reference editor definitions in runtime classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class UsesSpecificProcessDrawerAttribute : Attribute
    {
        /// <summary>
        /// The drawer's type.
        /// </summary>
        public string DrawerType { get; private set; }

        public UsesSpecificProcessDrawerAttribute(string drawerType)
        {
            DrawerType = drawerType;
        }
    }
}
