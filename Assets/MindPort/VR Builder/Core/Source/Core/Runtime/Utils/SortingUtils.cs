// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VRBuilder.Core.Utils
{
    /// <summary>
    /// Utilities related to sorting elements in collections.
    /// </summary>
    internal static class SortingUtils
    {
        /// <summary>
        /// Sorts elements using natural sort order.
        /// Example: (a1, a112, a12) => (a1, a12, a112).
        /// For reference, see https://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp.
        /// </summary>
        public static IOrderedEnumerable<T> OrderByAlphaNumericNaturalSort<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            IEnumerable<T> enumerable = source as T[] ?? source.ToArray();

            int max = 0;
            if (enumerable.Any())
            {
                max = enumerable.Select(i => FindMaxNumberInAlphaNumeric(selector(i))).Max();
            }

            return enumerable.OrderBy(i => UnifyNumbersInAlphaNumericToHaveEqualLengths(selector(i), max));
        }

        /// <summary>
        /// Comparer using the natural sort order.
        /// Example: (a1, a112, a12) => (a1, a12, a112).
        /// For reference, see https://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp.
        /// </summary>
        public class AlphaNumericNaturalSortComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return CompareToByAlphaNumeric(x, y);
            }
        }

        private static int CompareToByAlphaNumeric(string value1, string value2)
        {
            int longestNumberInCharacters = Math.Max(FindMaxNumberInAlphaNumeric(value1), FindMaxNumberInAlphaNumeric(value2));

            string comparableValue1 = UnifyNumbersInAlphaNumericToHaveEqualLengths(value1, longestNumberInCharacters);
            string comparableValue2 = UnifyNumbersInAlphaNumericToHaveEqualLengths(value2, longestNumberInCharacters);

            return string.Compare(comparableValue1, comparableValue2, StringComparison.InvariantCulture);
        }

        private static int FindMaxNumberInAlphaNumeric(string value)
        {
            return Regex.Matches(value, @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length).Max() ?? 0;
        }

        private static string UnifyNumbersInAlphaNumericToHaveEqualLengths(string value, int charsToPad)
        {
            return Regex.Replace(value, @"\d+", m => m.Value.PadLeft(charsToPad, '0'));
        }
    }
}
