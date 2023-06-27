// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace VRBuilder.Editor.PackageManager
{
    /// <summary>
    /// Utility class for adding missing layers to the Unity's TagManager.
    /// </summary>
    internal static class LayerUtils
    {
        /// <summary>
        /// Adds given <paramref name="layer"/> to the Unity's TagManager.
        /// </summary>
        public static void AddLayer(string layer)
        {
            string[] layers = {layer};
            AddLayers(layers);
        }

        /// <summary>
        /// Adds given <paramref name="layers"/> to the Unity's TagManager.
        /// </summary>
        /// <exception cref="FileLoadException">Exception thrown if the TagManager was not found.</exception>
        /// <exception cref="ArgumentException">Exception thrown if layers field is not found or is not an array.</exception>
        public static void AddLayers(IEnumerable<string> layers)
        {
            Object[] foundAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

            if (foundAsset.Any() == false)
            {
                throw new FileLoadException("There was a problem trying to load ProjectSettings/TagManager.asset");
            }

            SerializedObject tagManager = new SerializedObject(foundAsset.First());
            SerializedProperty layersField = tagManager.FindProperty("layers");
            Queue<string> newLayers = new Queue<string>(layers);

            if (layersField == null || layersField.isArray == false)
            {
                throw new ArgumentException("Field layers is either null or not array.");
            }

            // First 8 slots are reserved by Unity.
            for (int i = 8; i < layersField.arraySize; i++)
            {
                if (newLayers.Any())
                {
                    SerializedProperty serializedProperty = layersField.GetArrayElementAtIndex(i);
                    string stringValue = serializedProperty.stringValue;
                    string newLayer = newLayers.Peek();

                    if (stringValue == newLayer)
                    {
                        newLayers.Dequeue();
                        continue;
                    }

                    if (string.IsNullOrEmpty(stringValue))
                    {
                        serializedProperty.stringValue = newLayers.Dequeue();
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
        }
    }
}
