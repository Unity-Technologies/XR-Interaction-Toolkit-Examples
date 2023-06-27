// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Unity
{
    /// <summary>
    /// Allows to check if we are unit testing right now.
    /// </summary>
    internal static class UnitTestChecker
    {
        /// <summary>
        /// This will be set to true by RuntimeTests, if we are unit testing.
        /// </summary>
        public static bool IsUnitTesting { get; set; } = false;
    }
}
