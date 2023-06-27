// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.IO;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Editor.Configuration;
using UnityEngine;

namespace VRBuilder.Editor
{
    /// <summary>
    /// A collection of helper functions which are related to process asset handling.
    /// </summary>
    internal static class ProcessAssetUtils
    {
        /// <summary>
        /// Extracts the file name from the <paramref name="processPath"/>. Works with both relative and full paths.
        /// </summary>
        internal static string GetProcessNameFromPath(string processPath)
        {
            return Path.GetFileNameWithoutExtension(processPath);
        }

        /// <summary>
        /// Returns the asset path to the process with the <paramref name="processName"/>.
        /// </summary>
        internal static string GetProcessAssetPath(string processName)
        {
            return $"{GetProcessAssetDirectory(processName)}/{processName}.{EditorConfigurator.Instance.Serializer.FileFormat}";
        }

        /// <summary>
        /// Returns the relative path from the streaming assets directory to the process with the <paramref name="processName"/>.
        /// </summary>
        internal static string GetProcessStreamingAssetPath(string processName)
        {
            return $"{GetProcessStreamingAssetsSubdirectory(processName)}/{processName}.{EditorConfigurator.Instance.Serializer.FileFormat}";
        }

        /// <summary>
        /// Returns true if the file at given <paramref name="assetPath"/> is a process. It does not check the validity of the file's contents.
        /// </summary>
        internal static bool IsValidProcessAssetPath(string assetPath)
        {
            string filePath = Path.Combine(Application.dataPath.Remove(Application.dataPath.LastIndexOf('/')), assetPath).Replace('/', Path.DirectorySeparatorChar);
            string processFolderPath = Path.Combine(Application.streamingAssetsPath, EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory).Replace('/', Path.DirectorySeparatorChar);

            FileInfo file = new FileInfo(filePath);

            return
                file.Directory?.Parent != null
                && new DirectoryInfo(processFolderPath).FullName == file.Directory.Parent.FullName
                && Path.GetFileNameWithoutExtension(assetPath) == file.Directory.Name;
        }

        /// <summary>
        /// Returns a list of names of all processes in the project.
        /// </summary>
        internal static IEnumerable<string> GetAllProcesses()
        {
            DirectoryInfo processesDirectory = new DirectoryInfo($"{Application.streamingAssetsPath}/{EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory}");
            return processesDirectory.GetDirectories()
                .Select(directory => directory.Name)
                .Where(processName => File.Exists(GetProcessAssetPath(processName)))
                .ToList();
        }

        /// <summary>
        /// Checks if any process exists.
        /// </summary>
        internal static bool DoesAnyProcessExist()
        {
            DirectoryInfo processesDirectory = new DirectoryInfo($"{Application.streamingAssetsPath}/{EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory}");
            if (processesDirectory.Exists == false)
            {
                return false;
            }

            return GetAllProcesses().Any();
        }

        /// <summary>
        /// Checks if you can create a process with the given <paramref name="processName"/>.
        /// </summary>
        /// <param name="errorMessage">Empty if you can create the process or must fail silently.</param>
        internal static bool CanCreate(string processName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(processName))
            {
                errorMessage = "The process name is empty!";
                return false;
            }

            int invalidCharacterIndex;
            if ((invalidCharacterIndex = processName.IndexOfAny(Path.GetInvalidFileNameChars())) >= 0)
            {
                errorMessage = $"The process name contains invalid character `{processName[invalidCharacterIndex]}` at index {invalidCharacterIndex}";
                return false;
            }

            if (DoesProcessAssetExist(processName))
            {
                errorMessage = $"A process with the name \"{processName}\" already exists!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if you can rename the <paramref name="process"/> to the <paramref name="newName"/>.
        /// </summary>
        /// <param name="errorMessage">Empty if you can create the process or must fail silently.</param>
        internal static bool CanRename(IProcess process, string newName, out string errorMessage)
        {
            errorMessage = string.Empty;

            return process.Data.Name != newName && CanCreate(newName, out errorMessage);
        }

        /// <summary>
        /// Returns true if there is a process asset for the <paramref name="processName"/>.
        /// </summary>
        internal static bool DoesProcessAssetExist(string processName)
        {
            return File.Exists(GetProcessAssetPath(processName));
        }

        /// <summary>
        /// Returns the directory of the process <paramref name="processName"/> relative to the project root folder (Assets/StreamingAssets/...).
        /// </summary>
        internal static string GetProcessAssetDirectory(string processName)
        {
            return $"{Application.streamingAssetsPath}/{GetProcessStreamingAssetsSubdirectory(processName)}";
        }

        private static string GetProcessStreamingAssetsSubdirectory(string processName)
        {
            return $"{EditorConfigurator.Instance.ProcessStreamingAssetsSubdirectory}/{processName}";
        }
    }
}
