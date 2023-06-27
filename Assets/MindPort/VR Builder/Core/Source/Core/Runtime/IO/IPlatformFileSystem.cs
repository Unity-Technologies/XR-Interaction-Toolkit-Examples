// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VRBuilder.Core.IO
{
    /// <summary>
    /// Interface with basic platform operations for reading and saving files in Unity.
    /// </summary>
    /// <remarks>Operations are done for the StreamingAssets and platform persistent data folders.</remarks>
    public interface IPlatformFileSystem
    {
        /// <summary>
        /// Loads a file stored at <paramref name="filePath"/>.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the StreamingAssets or the persistent data folder.</remarks>
        /// <returns>The contents of the file into a byte array.</returns>
        /// <exception cref="FileNotFoundException">Exception thrown if the file does not exist.</exception>
        Task<byte[]> Read(string filePath);

        /// <summary>
        /// Loads a file stored at <paramref name="filePath"/>.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the StreamingAssets or the persistent data folder.</remarks>
        /// <returns>Returns a `string` with the content of the file.</returns>
        /// <exception cref="FileNotFoundException">Exception thrown if the file does not exist.</exception>
        Task<string> ReadAllText(string filePath);

        /// <summary>
        /// Saves given <paramref name="fileData"/> in provided <paramref name="filePath"/>.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to <see cref="PersistentDataPath"/>.</remarks>
        /// <returns>Returns true if <paramref name="fileData"/> could be saved successfully; otherwise, false.</returns>
        Task<bool> Write(string filePath, byte[] fileData);

        /// <summary>
        /// Returns true if given <paramref name="filePath"/> contains the name of an existing file under the StreamingAssets or platform persistent data folder; otherwise, false.
        /// </summary>
        /// <remarks><paramref name="filePath"/> must be relative to the StreamingAssets or the platform persistent data folder.</remarks>
        Task<bool> Exists(string filePath);

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory relative to the Streaming Assets folder.
        /// </summary>
        /// <param name="path">The relative path to the Streaming Assets folder. This string is not case-sensitive.</param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in <paramref name="path" />.
        /// Depending on the platform, this parameter can contain a combination of valid literal path and wildcard (* and ?) characters (see Remarks), but doesn't support regular expressions.
        /// </param>
        IEnumerable<string> FetchStreamingAssetsFilesAt(string path, string searchPattern);
    }
}
