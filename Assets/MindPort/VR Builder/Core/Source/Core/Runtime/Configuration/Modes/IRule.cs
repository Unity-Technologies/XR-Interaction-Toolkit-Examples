// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Core.Configuration.Modes
{
    /// <summary>
    /// Very generic interface which allows to implement a rule similar to Linq.Where or SQL Filter.
    /// </summary>
    public interface IRule<T>
    {
        /// <summary>
        /// Checks a given <paramref name="value"/> and returns true if this value qualifies the rule.
        /// </summary>
        /// <param name="value">The type which is checked.</param>
        bool IsQualifiedBy(T value);
    }
}
