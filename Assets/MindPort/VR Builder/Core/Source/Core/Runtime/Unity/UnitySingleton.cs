// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEngine;

namespace VRBuilder.Unity
{
    /// <summary>
    /// Make sure we always have one and only one instance of this class when we need it.
    /// </summary>
    public class UnitySingleton<T> : MonoBehaviour where T : UnitySingleton<T>
    {
        /// <summary>
        /// Semaphore to avoid instantiating the singleton twice.
        /// </summary>
        private static object _lock = new object();

        /// <summary>
        /// The actual instance of the singleton object.
        /// </summary>
        private static T instance;

        private static bool isApplicationQuitting = false;

        /// <summary>
        /// Public accessor for the singleton object.
        /// Will create a new instance if necessary.
        /// </summary>
        public static T Instance
        {
            get
            {
                // if we are shutting down right now, do not create a new instance
                if (isApplicationQuitting)
                {
                    return null;
                }
                lock (_lock)
                {
                    if (instance == null)
                    {
                        GameObject g = new GameObject();
                        g.name = "Singleton_ofType_" + typeof(T).ToString();
                        DontDestroyOnLoad(g);
                        instance =  g.AddComponent<T>();
                    }
                }
                return instance;
            }
            protected set
            {
                instance = value;
            }
        }

        protected virtual void Awake()
        {
            // make sure to assign the instance on awake
            if (instance == null)
            {
                instance = (T)this;
                DontDestroyOnLoad(instance);
            }
            else
            {
                if (Instance != this)
                {
                    Destroy(this);
                    Debug.LogWarningFormat("An instance of the singleton {0} already exists.", typeof(T).Name);
                } else
                {
                    DontDestroyOnLoad(instance);
                }
            }
        }

        protected virtual void OnDestroy()
        {
            // as soon as this singleton is about to be destroyed
            // make sure that no instance is returned / created anymore
            if (instance == this)
            {
                isApplicationQuitting = true;
            }
        }
    }
}
