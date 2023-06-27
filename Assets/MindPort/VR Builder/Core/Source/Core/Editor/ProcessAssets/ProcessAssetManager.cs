// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.IO;
using VRBuilder.Core;
using VRBuilder.Core.Serialization;
using VRBuilder.Editor.Configuration;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core.Configuration;

namespace VRBuilder.Editor
{
    /// <summary>
    /// A static class that handles the process assets. It lets you to save, load, delete, and import processes and provides multiple related utility methods.
    /// </summary>
    internal static class ProcessAssetManager
    {
        /// <summary>
        /// Deletes the process with <paramref name="processName"/>.
        /// </summary>
        internal static void Delete(string processName)
        {
            if (ProcessAssetUtils.DoesProcessAssetExist(processName))
            {
                Directory.Delete(ProcessAssetUtils.GetProcessAssetDirectory(processName), true);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Imports the given <paramref name="process"/> by saving it to the proper directory. If there is a name collision, this process will be renamed.
        /// </summary>
        internal static void Import(IProcess process)
        {
            int counter = 0;
            string oldName = process.Data.Name;
            while (ProcessAssetUtils.DoesProcessAssetExist(process.Data.Name))
            {
                if (counter > 0)
                {
                    process.Data.SetName(process.Data.Name.Substring(0, process.Data.Name.Length - 2));                    
                }

                counter++;
                process.Data.SetName(process.Data.Name + " " + counter);
            }

            if (oldName != process.Data.Name)
            {
                Debug.LogWarning($"We detected a name collision while importing process \"{oldName}\". We have renamed it to \"{process.Data.Name}\" before importing.");
            }

            Save(process);
        }

        /// <summary>
        /// Imports the process from file at given file <paramref name="path"/> if the file extensions matches the <paramref name="serializer"/>.
        /// </summary>
        internal static void Import(string path, IProcessSerializer serializer)
        {
            IProcess process;

            if (Path.GetExtension(path) != $".{serializer.FileFormat}")
            {
                Debug.LogError($"The file extension of {path} does not match the expected file extension of {serializer.FileFormat} of the current serializer.");
            }

            try
            {
                byte[] file = File.ReadAllBytes(path);
                process = serializer.ProcessFromByteArray(file);
            }
            catch (Exception e)
            {
                Debug.LogError($"{e.GetType().Name} occured while trying to import file '{path}' with serializer '{serializer.GetType().Name}'\n\n{e.StackTrace}");
                return;
            }

            Import(process);
        }

        /// <summary>
        /// Save the <paramref name="process"/> to the file system.
        /// </summary>
        internal static void Save(IProcess process)
        {
            try
            {
                string path = ProcessAssetUtils.GetProcessAssetPath(process.Data.Name);
                bool retvalue = AssetDatabase.MakeEditable(path);

                byte[] processData = EditorConfigurator.Instance.Serializer.ProcessToByteArray(process);
                WriteProcess(path, processData);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private static void WriteProcess(string path, byte[] processData)
        {
            FileStream stream = null;
            try
            {
                if(File.Exists(path))
                   File.SetAttributes(path, FileAttributes.Normal);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                stream = File.Create(path);
                stream.Write(processData, 0, processData.Length);
                stream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads the process with the given <paramref name="processName"/> from the file system and converts it into the <seealso cref="IProcess"/> instance.
        /// </summary>
        internal static IProcess Load(string processName)
        {
            if (ProcessAssetUtils.DoesProcessAssetExist(processName))
            {
                string processAssetPath = ProcessAssetUtils.GetProcessAssetPath(processName);
                byte[] processBytes = File.ReadAllBytes(processAssetPath);

                try
                {
                    return EditorConfigurator.Instance.Serializer.ProcessFromByteArray(processBytes);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load the process '{processName}' from '{processAssetPath}' because of: \n{ex.Message}");
                    Debug.LogError(ex);
                }
            }
            return null;
        }

        /// <summary>
        /// Renames the <paramref name="process"/> to the <paramref name="newName"/> and moves it to the appropriate directory. Check if you can rename before with the <seealso cref="CanRename"/> method.
        /// </summary>
        internal static void RenameProcess(IProcess process, string newName)
        {
            if (ProcessAssetUtils.CanRename(process, newName, out string errorMessage) == false)
            {
                Debug.LogError($"Process {process.Data.Name} was not renamed because:\n\n{errorMessage}");
                return;
            }

            string oldDirectory = ProcessAssetUtils.GetProcessAssetDirectory(process.Data.Name);
            string newDirectory = ProcessAssetUtils.GetProcessAssetDirectory(newName);

            Directory.Move(oldDirectory, newDirectory);
            File.Move($"{oldDirectory}.meta", $"{newDirectory}.meta");

            string newAsset = ProcessAssetUtils.GetProcessAssetPath(newName);
            string oldAsset = $"{ProcessAssetUtils.GetProcessAssetDirectory(newName)}/{process.Data.Name}.{EditorConfigurator.Instance.Serializer.FileFormat}";
            File.Move(oldAsset, newAsset);
            File.Move($"{oldAsset}.meta", $"{newAsset}.meta");
            process.Data.SetName(newName);

            Save(process);

            RuntimeConfigurator.Instance.SetSelectedProcess(newAsset);
        }
    }
}
