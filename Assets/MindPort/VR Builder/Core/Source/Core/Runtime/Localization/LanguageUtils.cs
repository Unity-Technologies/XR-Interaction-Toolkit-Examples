// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace VRBuilder.Core.Internationalization
{
    /// <summary>
    /// Collection of language utilities.
    /// </summary>
    public static class LanguageUtils
    {
        /// <summary>
        /// Convert natural language name to two-letters ISO code.
        /// </summary>
        /// <param name="language">
        /// String with natural language name or two-letters ISO code.
        /// </param>
        /// <param name="result">
        /// If <paramref name="language"/> is already in two-letters ISO code, simply returns it.
        /// If <paramref name="language"/> is a natural language name, returns two-symbol code.
        /// Otherwise, returns null.
        /// </param>
        /// <returns>
        /// Was operation successful or not.
        /// </returns>
        public static bool TryConvertToTwoLetterIsoCode(this string language, out string result)
        {
            if (IsTwoLettersIsoCode(language))
            {
                result = language.ToLower();
                return true;
            }

            try
            {
                result = ConvertNaturalLanguageNameToTwoLetterIsoCode(language);
                return true;
            }
            catch (ArgumentException)
            {
                result = null;
                return false;

            }
        }

        /// <summary>
        /// Helps to convert strings with full language names like "English" to a two-letter ISO language code.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="languageName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="languageName"/> is not natural language name.</exception>
        /// <returns>The two-letter ISO code from the given language name. If it can not parse the string, it returns null.</returns>
        private static string ConvertNaturalLanguageNameToTwoLetterIsoCode(this string languageName)
        {
            if (languageName == null)
            {
                throw new ArgumentNullException("languageName", "languageName is null");
            }

            IEnumerable<CultureInfo> allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            CultureInfo languageCulture = allCultures.FirstOrDefault(culture =>
            {
                string preparedCultureName = culture.EnglishName.RemoveSymbols('(', ')', ' ');
                return string.Compare(preparedCultureName, languageName, StringComparison.OrdinalIgnoreCase) == 0;
            });

            if (languageCulture != null)
            {
                return languageCulture.TwoLetterISOLanguageName;
            }

            throw new ArgumentException("languageName is not a supported language name", "languageName");
        }


        /// <summary>
        /// Check if <paramref name="language"/> is two-letter ISO code.
        /// </summary>
        private static bool IsTwoLettersIsoCode(string language)
        {
            if (language == null)
            {
                return false;
            }

            // Some two-letter ISO codes are three letters long.
            if (language.Length < 2 || language.Length > 3)
            {
                return false;
            }

            try
            {
                // If CultureInfo constructor is able to parse the string, it's two-letter ISO code.
                // ReSharper disable once ObjectCreationAsStatement
                new CultureInfo(language);
                return true;
            }
            catch (ArgumentException)
            {
                // Otherwise, it isn't.
                return false;
            }
        }

        /// <summary>
        /// Remove <paramref name="symbolsToRemove"/> from <paramref name="input"/> string.
        /// </summary>
        private static string RemoveSymbols(this string input, params char[] symbolsToRemove)
        {
            string result = input;
            foreach (char symbol in symbolsToRemove)
            {
                result = result.Replace(symbol.ToString(), "");
            }

            return result;
        }
    }
}
