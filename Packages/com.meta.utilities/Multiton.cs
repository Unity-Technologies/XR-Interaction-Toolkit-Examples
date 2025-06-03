// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.Utilities
{
    public class Multiton<T> : MonoBehaviour where T : Multiton<T>
    {
        protected static readonly HashSet<T> InternalInstances = new();

        public struct ReadOnlyList : IEnumerable<T>
        {
            public HashSet<T>.Enumerator GetEnumerator() => InternalInstances.GetEnumerator();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static ReadOnlyList Instances => new();

        protected void Awake()
        {
            if (!enabled)
                return;
            _ = InternalInstances.Add((T)this);
        }

        protected void OnEnable()
        {
            _ = InternalInstances.Add((T)this);
        }

        protected void OnDisable()
        {
            _ = InternalInstances.Remove((T)this);
        }

        protected void OnDestroy()
        {
            _ = InternalInstances.Remove((T)this);
        }
    }
}
