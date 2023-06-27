// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace VRBuilder.Core.Runtime.Utils
{
    /// <summary>
    /// ScriptableObject with additional load and save mechanic to make it a singleton.
    /// </summary>
    /// <typeparam name="T">The class itself</typeparam>
    public class SettingsObject<T> : ScriptableObject where T : ScriptableObject, new()
    {
        private static T instance;

        public static T Instance
        {
            get
            {

#if UNITY_EDITOR
                if (EditorUtility.IsDirty(instance))
                {
                    instance = null;
                }
#endif
                if (instance == null)
                {
                    instance = Load();
                }

                return instance;
            }
        }

        private static T Load()
        {
            T settings = Resources.Load<T>(typeof(T).Name);

            if (settings == null)
            {
                // Create an instance
                settings = CreateInstance<T>();
#if UNITY_EDITOR
                if (!Directory.Exists("Assets/MindPort/VR Builder/Resources"))
                {
                    Directory.CreateDirectory("Assets/MindPort/VR Builder/Resources");
                }
                AssetDatabase.CreateAsset(settings, $"Assets/MindPort/VR Builder/Resources/{typeof(T).Name}.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#endif
            }
            return settings;
        }

        /// <summary>
        /// Saves the VR Builder settings, only works in editor.
        /// </summary>
        public void Save()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        ~SettingsObject()
        {
#if UNITY_EDITOR
            if (EditorUtility.IsDirty(this))
            {
                Save();
            }
#endif
        }
    }
}
