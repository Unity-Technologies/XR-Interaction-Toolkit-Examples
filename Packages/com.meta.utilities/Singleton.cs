// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        private static System.Action<T> s_onAwake;

        public static void WhenInstantiated(System.Action<T> action)
        {
            if (Instance != null)
                action(Instance);
            else
                s_onAwake += action;
        }

        protected void Awake()
        {
            if (!enabled)
                return;

            Debug.Assert(Instance == null, $"Singleton {typeof(T).Name} has been instantiated more than once.", this);
            Instance = (T)this;

            InternalAwake();

            s_onAwake?.Invoke(Instance);
            s_onAwake = null;
        }

        protected virtual void OnEnable()
        {
            if (Instance != this)
                Awake();
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        protected virtual void InternalAwake() { }
    }
}
