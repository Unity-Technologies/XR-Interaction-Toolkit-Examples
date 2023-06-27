// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRBuilder.Core.Utils;
using VRBuilder.Core.Serialization;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.BuilderMenu
{
    internal static class ImportProcessMenuEntry
    {
        /// <summary>
        /// Allows to import processes.
        /// </summary>
        [MenuItem("Tools/VR Builder/Import Process...", false, 14)]
        private static void ImportProcess()
        {
            string path = EditorUtility.OpenFilePanel("Select your process", ".", String.Empty);

            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            {
                return;
            }

            string format = Path.GetExtension(path).Replace(".", "");
            List<IProcessSerializer> result = GetFittingSerializer(format);

            if (result.Count == 0)
            {
                Debug.LogError("Tried to import, but no Serializer found.");
                return;
            }

            if (result.Count == 1)
            {
                ProcessAssetManager.Import(path, result.First());
            }
            else
            {
                ChooseSerializerPopup.Show(result, (serializer) =>
                {
                    ProcessAssetManager.Import(path, serializer);
                });
            }
        }

        private static List<IProcessSerializer> GetFittingSerializer(string format)
        {
            return ReflectionUtils.GetConcreteImplementationsOf<IProcessSerializer>()
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => (IProcessSerializer)ReflectionUtils.CreateInstanceOfType(type))
                .Where(s => s.FileFormat.Equals(format))
                .ToList();
        }
    }
}
