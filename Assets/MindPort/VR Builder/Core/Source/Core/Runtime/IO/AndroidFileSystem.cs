// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

#if !UNITY_EDITOR && UNITY_ANDROID
using UnityEngine;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VRBuilder.Core.IO
{
    /// <summary>
    /// Android implementation of <see cref="IPlatformFileSystem"/>.
    /// </summary>
    public class AndroidFileSystem : DefaultFileSystem
    {
        private readonly string rootFolder;
        private readonly IEnumerable<string> cachedStreamingAssetsFilesPath;

        private const string StreamingAssetsArchivePath = "assets/";
        private const string ExcludedArchivePath = "assets/bin/";

        public AndroidFileSystem(string streamingAssetsPath, string persistentDataPath) : base(streamingAssetsPath, persistentDataPath)
        {
            rootFolder = Application.dataPath;

            using (ZipArchive archive = ZipFile.OpenRead(rootFolder))
            {
                cachedStreamingAssetsFilesPath = archive.Entries.Select(entry => entry.FullName)
                    .Where(name => name.StartsWith(StreamingAssetsArchivePath))
                    .Where(name => name.StartsWith(ExcludedArchivePath) == false);
            }
        }

        /// <inheritdoc />
        public override async Task<string> ReadAllText(string filePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(rootFolder))
            {
                string relativePath = filePath.StartsWith(StreamingAssetsArchivePath) ? filePath : Path.Combine("assets", filePath);
                ZipArchiveEntry file = archive.Entries.First(entry => entry.FullName == relativePath);

                if (file == null)
                {
                    throw new FileNotFoundException(relativePath);
                }

                using (Stream fileStream = file.Open())
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>In Android, <paramref name="searchPattern"/> does not support wildcard characters.</remarks>
        public override IEnumerable<string> FetchStreamingAssetsFilesAt(string path, string searchPattern)
        {
            string relativePath = Path.Combine("assets", path);
            string[] wildCardChars = { "?", "_", "*", "%", "#" };
            searchPattern = wildCardChars.Aggregate(searchPattern, (current, wildCardChar) => current.Replace(wildCardChar, string.Empty));

            return cachedStreamingAssetsFilesPath.Where(filePath => filePath.StartsWith(relativePath) && filePath.Contains(searchPattern));
        }

        /// <inheritdoc />
        /// <remarks>This method uses additional functionality of 'System.IO.Compression' that are not bundled with Unity.
        /// The MSBuild response file 'Assets/csc.rsp' is required for this.
        /// See more: https://docs.unity3d.com/Manual/dotnetProfileAssemblies.html</remarks>
        protected override async Task<byte[]> ReadFromStreamingAssets(string filePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(rootFolder))
            {
                string relativePath = filePath.StartsWith(StreamingAssetsArchivePath) ? filePath : Path.Combine("assets", filePath);
                ZipArchiveEntry file = archive.Entries.First(entry => entry.FullName == relativePath);

                if (file == null)
                {
                    throw new FileNotFoundException(relativePath);
                }

                using (Stream fileStream = file.Open())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        fileStream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override async Task<bool> FileExistsInStreamingAssets(string filePath)
        {
            return cachedStreamingAssetsFilesPath.Any(path => path == filePath);
        }

        /// <inheritdoc />
        protected override string NormalizePath(string filePath)
        {
            filePath = Path.Combine(StreamingAssetsArchivePath, filePath);
            return base.NormalizePath(filePath);
        }
    }
}
#endif
