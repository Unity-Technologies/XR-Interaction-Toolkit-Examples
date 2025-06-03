// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Meta.Utilities
{
    [Serializable]
    public class EnumDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : struct, Enum
    {
        static EnumDictionary()
        {
            foreach (var key in AllKeys)
            {
                CheckBounds(key);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void CheckBounds(TKey key)
        {
            var val = ToInt32(key);
            if (val < 0 || val >= Length)
            {
                Debug.LogAssertion($"EnumDictionary key {typeof(TKey).Name}.{key} is out of bounds.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt32(TKey key) => UnsafeUtility.EnumToInt(key);

        protected static TKey[] AllKeys { get; } = (TKey[])typeof(TKey).GetEnumValues();
        protected static int Length { get; } = AllKeys.Max(e => ToInt32(e)) + 1;

        [SerializeField]
        protected TValue[] m_values = new TValue[Length];

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckBounds(key);
                return m_values[ToInt32(key)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_values[ToInt32(key)] = value;
        }

        public ReadOnlyCollection<TKey> Keys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Array.AsReadOnly(AllKeys);
        }

        public TValue[] Values
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_values;
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Array.AsReadOnly(AllKeys);
        ICollection<TValue> IDictionary<TKey, TValue>.Values => m_values;

        public int Count => Length;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => this[key] = value;
        public void Add(KeyValuePair<TKey, TValue> item) => this[item.Key] = item.Value;

        public bool Contains(KeyValuePair<TKey, TValue> item) => EqualityComparer<TValue>.Default.Equals(this[item.Key], item.Value);
        public bool ContainsKey(TKey key) => true;

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            internal EnumDictionary<TKey, TValue> m_dictionary;
            internal ArraySegment<TKey>.Enumerator m_keyEnumerator;

            public KeyValuePair<TKey, TValue> Current => new(m_keyEnumerator.Current, m_dictionary[m_keyEnumerator.Current]);
            object IEnumerator.Current => Current;

            public void Dispose() => m_keyEnumerator.Dispose();
            public bool MoveNext() => m_keyEnumerator.MoveNext();
            public void Reset() => ((IEnumerator<TKey>)m_keyEnumerator).Reset();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => Pairs.ToArray().CopyTo(array, arrayIndex);
        public Enumerator GetEnumerator() => new() { m_dictionary = this, m_keyEnumerator = new ArraySegment<TKey>(AllKeys).GetEnumerator() };
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected IEnumerable<KeyValuePair<TKey, TValue>> Pairs => GetEnumerator().AsEnumerable();

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = this[key];
            return true;
        }

        public void Clear() => throw new NotImplementedException();
        public bool Remove(TKey key) => throw new NotImplementedException();
        public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotImplementedException();
    }
}
