/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

namespace Oculus.Interaction.Collections
{
    /// <summary>
    /// Exposes a GetEnumerator method with a non-allocating
    /// HashSet.Enumerator struct.
    /// </summary>
    public interface IEnumerableHashSet<T> : IEnumerable<T>
    {
        int Count { get; }
        new HashSet<T>.Enumerator GetEnumerator();
        bool Contains(T item);
        bool IsProperSubsetOf(IEnumerable<T> other);
        bool IsProperSupersetOf(IEnumerable<T> other);
        bool IsSubsetOf(IEnumerable<T> other);
        bool IsSupersetOf(IEnumerable<T> other);
        public bool Overlaps(IEnumerable<T> other);
        public bool SetEquals(IEnumerable<T> other);
    }

    /// <summary>
    /// A Hash set that implements the <see cref="IEnumerableHashSet{T}"/>
    /// interface, to use for non-allocating iteration of a HashSet
    /// </summary>
    public class EnumerableHashSet<T> : HashSet<T>, IEnumerableHashSet<T>
    {
        public EnumerableHashSet() : base() { }
        public EnumerableHashSet(IEnumerable<T> values) : base(values) { }
    }
}
