/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if !UNITY_2021_1_OR_NEWER
using System.Collections.Generic;
#endif
using System.Text.RegularExpressions;

namespace Meta.Conduit
{
    /// <summary>
    /// Utility class for Conduit.
    /// </summary>
    internal static class ConduitUtilities
    {
        /// <summary>
        /// A delegate for reporting progress. The progress value range is 0.0f to 1.0f.
        /// </summary>
        public delegate void ProgressDelegate(string status, float progress);

        /// <summary>
        /// The Regex pattern for splitting on underscores.
        /// </summary>
        private static readonly Regex UnderscoreSplitter = new Regex("(\\B[A-Z])", RegexOptions.Compiled);

        /// <summary>
        /// Splits a string at word boundaries and delimits it with underscores.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string DelimitWithUnderscores(string input)
        {
            return UnderscoreSplitter.Replace(input, "_$1");
        }

        /// <summary>
        /// Returns true if <paramref name="stringToSearch"/> contains <paramref name="value"/> when ignoring whitespace.
        /// </summary>
        /// <param name="stringToSearch">The string to search it.</param>
        /// <param name="value">The substring to look for.</param>
        /// <returns>True if found. False otherwise.</returns>
        public static bool ContainsIgnoringWhitespace(string stringToSearch, string value)
        {
            stringToSearch = StripWhiteSpace(stringToSearch);
            value = StripWhiteSpace(value);
            return stringToSearch.Contains(value);
        }

        private static string StripWhiteSpace(string input)
        {
            return string.IsNullOrEmpty(input) ? string.Empty :
                input.Replace(" ", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);
        }

        /// <summary>
        /// Get sanitized entity class name
        /// </summary>
        /// <param name="entityRole">Role of an entity</param>
        /// <returns>Entity class name for specified role</returns>
        public static string GetEntityEnumName(string entityRole)
        {
            return SanitizeName(entityRole);
        }

        /// <summary>
        /// Get sanitized entity value
        /// </summary>
        /// <param name="entityValue">The value of the entity.</param>
        /// <returns></returns>
        public static string GetEntityEnumValue(string entityValue)
        {
            return SanitizeString(entityValue);
        }

        /// <summary>
        /// Script that sanitizes string values
        /// to ensure they can be used as a class name
        /// </summary>
        /// <param name="input">Initial string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeName(string input)
        {
            // Ensure no empty/null names
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            // Standard sanitize
            var result = SanitizeString(input);
            // Capitalize first letter
            return result[0].ToString().ToUpper() + result.Substring(1);
        }

        /// <summary>
        /// Script that sanitizes string values
        /// to ensure they can be used in code
        /// </summary>
        /// <param name="input">Initial string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeString(string input)
        {
            // Ensure no empty/null strings
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            // Remove all non word characters, underscore & hyphen
            var result = Regex.Replace(input, @"[^\w_-]", "");
            // Starts with number, append N
            if (Regex.IsMatch(result[0].ToString(), @"^\d$"))
            {
                result = $"N{result}";
            }
            // Sanitized string
            return result;
        }
    }

#if !UNITY_2021_1_OR_NEWER
    internal static class ListExtensions
    {
        public static HashSet<T> ToHashSet<T> (this List<T> source)
        {
            var output = new HashSet<T>();
            foreach (var element in source)
            {
                output.Add(element);
            }

            return output;
        }
    }
    #endif
}
