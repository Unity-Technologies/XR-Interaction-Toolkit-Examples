// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

namespace VRBuilder.Unity
{
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static T instance = new T();

        public static T Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
