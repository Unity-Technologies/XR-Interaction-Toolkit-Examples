/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Linq;

namespace Meta.WitAi
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Compares two collections while handling null cases and returns true if they are equal or both null.
        /// </summary>
        /// <param name="first">The first collection.</param>
        /// <param name="second">The second collection.</param>
        /// <typeparam name="TSource">The type of the collection.</typeparam>
        /// <returns>True if both have the same elements or are both null.</returns>
        internal static bool Equivalent<TSource>(this System.Collections.Generic.IEnumerable<TSource> first,
            System.Collections.Generic.IEnumerable<TSource> second)
        {
            if (first == null && second == null)
            {
                return true;
            }

            if (first == null || second == null)
            {
                return false;
            }

            return first.SequenceEqual(second);
        }
    }
}
