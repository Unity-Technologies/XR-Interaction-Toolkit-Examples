// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// All listed types will be valid.
    /// </summary>
    /// <typeparam name="TBase">Type which can be filtered.</typeparam>
    public class WhitelistTypeRule<TBase> : ListTypeRule<WhitelistTypeRule<TBase>, TBase>
    {
        /// <inheritdoc />
        protected override bool IsQualifiedByPredicate(Type type)
        {
            return StoredTypes.Contains(type);
        }
    }
}
