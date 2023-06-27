// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRBuilder.Core.IO
{
    /// <summary>
    /// Default implementation of <see cref="IPlatformFileSystem"/> based in 'System.IO'.
    /// </summary>
    /// <remarks>It works out of the box for most of the Unity's supported platforms.</remarks>
    public class DefaultFileSystem : IPlatformFileSystem
    {
        /// <summary>
        /// The path to the platform's StreamingAssets folder (Read Only).
        /// </summary>
        protected readonly string StreamingAssetsPath;

        /// <summary>
        /// The path to the platform's persistent data directory (Read Only).
        /// </summary>
        protected readonly string PersistentDataPath;

        public DefaultFileSystem(string streamingAssetsPath, string persistentDataPath)
        {
            StreamingAssetsPath = streamingAssetsPath;
            PersistentDataPath = persistentDataPath;
        }

        /// <inheritdoc />
        public virtual async Task<byte[]> Read(string filePath)
        {
            filePath = NormalizePath(filePath);

            if (await FileExistsInStreamingAssets(filePath))
            {
                return await ReadFromStreamingAssets(filePath);
            }

            if (await FileExistsInPersistentData(filePath))
            {
                return await ReadFromPersistentData(filePath);
            }

            throw new FileNotFoundException(filePath);
        }

        /// <inheritdoc />
        public virtual async Task<string> ReadAllText(string filePath)
        {
            filePath = NormalizePath(filePath);

            if (await Exists(filePath))
            {
                string rootPath = await FileExistsInStreamingAssets(filePath) ? Application.streamingAssetsPath : Application.persistentDataPath;
                string absolutePath = Path.Combine(rootPath, filePath);
                return File.ReadAllText(absolutePath);
            }

            throw new FileNotFoundException(filePath);
        }

        /// <inheritdoc />
#pragma warning disable 1998
        public virtual async Task<bool> Write(string filePath, byte[] fileData)
#pragma warning restore
        {
            filePath = NormalizePath(filePath);

            try
            {
                string absoluteFilePath = BuildPersistentDataPath(filePath);
                File.WriteAllBytes(absoluteFilePath, fileData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <inheritdoc />
        public virtual async Task<bool> Exists(string filePath)
        {
            filePath = NormalizePath(filePath);
            return await FileExistsInStreamingAssets(filePath) || await FileExistsInPersistentData(filePath);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The following wildcard specifiers are permitted in <paramref name="searchPattern"/>:
        /// Wildcard specifier	    Matches
        /// * (asterisk)	        Zero or more characters in that position.
        /// ? (question mark)	    Zero or one character in that position.
        /// </remarks>
        public virtual IEnumerable<string> FetchStreamingAssetsFilesAt(string path, string searchPattern)
        {
            string relativePath = Path.Combine(StreamingAssetsPath, path);
            return Directory.GetFiles(relativePath, searchPattern);
        }

        /// <summary>
        /// Loads a file stored at <paramref name="filePath"/>.
        /// Returns a `FileNotFoundException` if file does not exist.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the StreamingAssets folder.</remarks>
        /// <returns>The contents of the file into a byte array.</returns>
#pragma warning disable 1998
        protected virtual async Task<byte[]> ReadFromStreamingAssets(string filePath)
#pragma warning restore
        {
            string absolutePath = Path.Combine(StreamingAssetsPath, filePath);

            if (File.Exists(absolutePath) == false)
            {
                throw new FileNotFoundException($"File at path '{filePath}' could not be found.");
            }

            return File.ReadAllBytes(absolutePath);
        }

        /// <summary>
        /// Loads a file stored at <paramref name="filePath"/>.
        /// Returns a `FileNotFoundException` if file does not exist.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the platform persistent data folder.</remarks>
        /// <returns>The contents of the file into a byte array.</returns>
        protected virtual async Task<byte[]> ReadFromPersistentData(string filePath)
        {
            string absolutePath = Path.Combine(PersistentDataPath, filePath);

            if (await FileExistsInPersistentData(filePath) == false)
            {
                throw new FileNotFoundException($"File at path '{absolutePath}' could not be found.");
            }

            return File.ReadAllBytes(absolutePath);
        }

        /// <summary>
        /// Returns true if given <paramref name="filePath"/> contains the name of an existing file under the StreamingAssets folder; otherwise, false.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the StreamingAssets folder.</remarks>
#pragma warning disable 1998
        protected virtual async Task<bool> FileExistsInStreamingAssets(string filePath)
#pragma warning restore
        {
            string absolutePath = Path.Combine(StreamingAssetsPath, filePath);
            return File.Exists(absolutePath);
        }

        /// <summary>
        /// Returns true if given <paramref name="filePath"/> contains the name of an existing file under the platform persistent data folder; otherwise, false.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the platform persistent data folder.</remarks>
#pragma warning disable 1998
        protected virtual async Task<bool> FileExistsInPersistentData(string filePath)
#pragma warning restore
        {
            string absolutePath = Path.Combine(PersistentDataPath, filePath);
            return File.Exists(absolutePath);
        }

        /// <summary>
        /// Builds a directory from given <paramref name="filePath"/>.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the platform persistent data folder.</remarks>
        /// <returns>The created directory absolute path.</returns>
        protected virtual string BuildPersistentDataPath(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string relativePath = Path.GetDirectoryName(filePath);

            string absolutePath = Path.Combine(PersistentDataPath, relativePath);

            if (Directory.Exists(absolutePath) == false)
            {
                Directory.CreateDirectory(absolutePath);
                Debug.LogWarningFormat("Directory '{0}' was created.", absolutePath);
            }

            if (string.IsNullOrEmpty(fileName))
            {
                return absolutePath;
            }

            return Path.Combine(PersistentDataPath, filePath);
        }

        /// <summary>
        /// Normalizes path to platform specific.
        /// </summary>
        protected virtual string NormalizePath(string filePath)
        {
            return filePath.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
