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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

/// <summary>
/// Allows you to enumerate an IEnumerable in a non allocating way, if possible.
/// </summary>
/// <typeparam name="T">The type of item contained by the collection.</typeparam>
/// <seealso cref="OVRExtensions.ToNonAlloc{T}"/>
internal readonly struct OVREnumerable<T> : IEnumerable<T>
{
    readonly IEnumerable<T> _enumerable;

    public OVREnumerable(IEnumerable<T> enumerable) => _enumerable = enumerable;

    public Enumerator GetEnumerator() => new Enumerator(_enumerable);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        enum CollectionType
        {
            None,
            List,
            Set,
            Queue,
            Enumerable,
        }

        int _listIndex;
        readonly CollectionType _type;
        readonly int _listCount;
        readonly IEnumerator<T> _enumerator;
        readonly IReadOnlyList<T> _list;
        HashSet<T>.Enumerator _setEnumerator;
        Queue<T>.Enumerator _queueEnumerator;

        public Enumerator(IEnumerable<T> enumerable)
        {
            _setEnumerator = default;
            _queueEnumerator = default;
            _enumerator = null;
            _list = null;
            _listIndex = -1;
            _listCount = 0;

            switch (enumerable)
            {
                case IReadOnlyList<T> list:
                    _list = list;
                    _listCount = list.Count;
                    _type = CollectionType.List;
                    break;
                case HashSet<T> set:
                    _setEnumerator = set.GetEnumerator();
                    _type = CollectionType.Set;
                    break;
                case Queue<T> queue:
                    _queueEnumerator = queue.GetEnumerator();
                    _type = CollectionType.Queue;
                    break;
                default:
                    _enumerator = enumerable.GetEnumerator();
                    _type = CollectionType.Enumerable;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _type switch
        {
            CollectionType.List => MoveNextList(),
            CollectionType.Set => _setEnumerator.MoveNext(),
            CollectionType.Queue => _queueEnumerator.MoveNext(),
            CollectionType.Enumerable => _enumerator.MoveNext(),
            _ => throw new InvalidOperationException($"Unsupported collection type {_type}.")
        };

        bool MoveNextList()
        {
            ValidateAndThrow();
            return ++_listIndex < _listCount;
        }

        public void Reset()
        {
            switch (_type)
            {
                case CollectionType.List:
                    ValidateAndThrow();
                    _listIndex = -1;
                    break;
                case CollectionType.Set:
                case CollectionType.Queue:
                    break;
                case CollectionType.Enumerable:
                    _enumerator.Reset();
                    break;
            }
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _type switch
            {
                CollectionType.List => _list[_listIndex],
                CollectionType.Set => _setEnumerator.Current,
                CollectionType.Queue => _queueEnumerator.Current,
                CollectionType.Enumerable => _enumerator.Current,
                _ => throw new InvalidOperationException($"Unsupported collection type {_type}.")
            };
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            switch (_type)
            {
                case CollectionType.List:
                    break;
                case CollectionType.Set:
                    _setEnumerator.Dispose();
                    break;
                case CollectionType.Queue:
                    _queueEnumerator.Dispose();
                    break;
                case CollectionType.Enumerable:
                    _enumerator.Dispose();
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ValidateAndThrow()
        {
            if (_listCount != _list.Count)
                throw new InvalidOperationException($"The list changed length during enumeration.");
        }
    }
}

static partial class OVRExtensions
{
    /// <summary>
    /// Allows the caller to enumerate an IEnumerable in a non-allocating way, if possible.
    /// </summary>
    /// <remarks>
    /// <example>
    /// If you have an IEnumerable, this will allocate IEnumerator:
    /// <code><![CDATA[
    /// void Foo(IEnumerable<T> collection) {
    ///   // Allocates an IEnumerator<T>
    ///   foreach (var item in collection) {
    ///     // do something with item
    ///   }
    /// }
    /// ]]></code>
    /// However, often the IEnumerable is at least an IReadOnlyList, e.g., a List or Array, its elements can be accessed
    /// using the index operator. This custom enumerable will do that:
    /// <code><![CDATA[
    /// void Foo(IEnumerable<T> collection) {
    ///   // Returns a non-allocating struct-based enumerator
    ///   foreach (var item in collection.ToNonAlloc()) {
    ///     // do something with item
    ///   }
    /// }
    /// ]]></code>
    /// </example>
    ///
    /// Note that some safeties cannot be guaranteed, such as mutations to a List during enumeration.
    /// </remarks>
    /// <param name="enumerable">The collection you wish to enumerate.</param>
    /// <typeparam name="T">The type of item in the collection.</typeparam>
    /// <returns>Returns a non-allocating enumerable.</returns>
    internal static OVREnumerable<T> ToNonAlloc<T>(this IEnumerable<T> enumerable) => new OVREnumerable<T>(enumerable);

    /// <summary>
    /// Copies a collection to a `NativeArray`.
    /// </summary>
    /// <remarks>
    /// This will copy <paramref name="enumerable"/> to a NativeArray in the most efficient way possible. Behavior of
    /// <paramref name="enumerable"/> in order of decreasing efficiency:
    /// - Fixed-size array: single native allocation + memcpy - no managed allocations
    /// - IReadOnlyList: single native allocation + iteration - no managed allocations
    /// - HashSet: single native allocation + iteration - no managed allocations
    /// - Queue: single native allocation + iteration - no managed allocations
    /// - IReadOnlyCollection: single native allocation - single managed IEnumerator allocation
    /// - ICollection: single native allocation - single managed IEnumerator allocation
    /// - Anything else: multiple native allocations (using a growth strategy) - single managed IEnumerator allocation
    /// </remarks>
    /// <param name="enumerable">The collection to copy to a NativeArray</param>
    /// <param name="allocator">The allocator to use for the returned NativeArray</param>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <returns>Returns a new NativeArray allocated with <paramref name="allocator"/> filled with the elements of
    /// <paramref name="enumerable"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="enumerable"/> is `null`.</exception>
    internal static NativeArray<T> ToNativeArray<T>(this IEnumerable<T> enumerable, Allocator allocator)
        where T : struct
    {
        if (enumerable == null)
            throw new ArgumentNullException(nameof(enumerable));

        switch (enumerable)
        {
            // Easiest case, since NativeArray supports this
            case T[] fixedArray: return new NativeArray<T>(fixedArray, allocator);

            // Good, since we can iterate the list without allocating
            case IReadOnlyList<T> list:
            {
                var array = new NativeArray<T>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = list[i];
                }

                return array;
            }

            // HashSet can be iterated without allocation but doesn't conform to any interface that supports it, so
            // it's a special case.
            case HashSet<T> set:
            {
                var array = new NativeArray<T>(set.Count, allocator, NativeArrayOptions.UninitializedMemory);
                var index = 0;
                foreach (var item in set)
                {
                    array[index++] = item;
                }

                return array;
            }

            // Same as HashSet
            case Queue<T> queue:
            {
                var array = new NativeArray<T>(queue.Count, allocator, NativeArrayOptions.UninitializedMemory);
                var index = 0;
                foreach (var item in queue)
                {
                    array[index++] = item;
                }

                return array;
            }

            // Less good because we need to allocate to iterate, but we can know the size beforehand
            case IReadOnlyCollection<T> collection:
            {
                var array = new NativeArray<T>(collection.Count, allocator, NativeArrayOptions.UninitializedMemory);
                var index = 0;
                foreach (var item in collection)
                {
                    array[index++] = item;
                }

                return array;
            }

            // Same as above
            case ICollection<T> collection:
            {
                var array = new NativeArray<T>(collection.Count, allocator, NativeArrayOptions.UninitializedMemory);
                var index = 0;
                foreach (var item in collection)
                {
                    array[index++] = item;
                }

                return array;
            }

            // Fallback to worst case, but only enumerate the collection once
            default:
            {
                var count = 0;
                var capacity = 4;
                var array = new NativeArray<T>(capacity, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                foreach (var item in enumerable)
                {
                    if (count == capacity)
                    {
                        // Grow the array
                        capacity *= 2;
                        NativeArray<T> newArray;
                        using (array)
                        {
                            newArray = new NativeArray<T>(capacity, Allocator.Temp,
                                NativeArrayOptions.UninitializedMemory);
                            NativeArray<T>.Copy(array, newArray, array.Length);
                        }

                        array = newArray;
                    }

                    array[count++] = item;
                }

                using (array)
                {
                    var result = new NativeArray<T>(count, allocator, NativeArrayOptions.UninitializedMemory);
                    NativeArray<T>.Copy(array, result, count);
                    return result;
                }
            }
        }
    }
}
