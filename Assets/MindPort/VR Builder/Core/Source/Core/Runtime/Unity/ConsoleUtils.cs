// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Unity
{
    internal static class ConsoleUtils
    {
        private const string tab = "    ";

        /// <summary>
        /// Returns a string containing tabs. One tab is four spaces.
        /// </summary>
        /// <param name="tabsCount">Amount of tabs.</param>
        /// <returns></returns>
        public static string GetTabs(uint tabsCount = 1)
        {
            string result = "";

            for (uint i = 0; i < tabsCount; i++)
            {
                result += tab;
            }

            return result;
        }
    }
}
