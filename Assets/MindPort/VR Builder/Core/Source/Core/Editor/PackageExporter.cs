// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using VRBuilder.Unity;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor
{
    internal static class PackageExporter
    {
        private class PackageExporterArguments
        {
            [Option("export-config", MetaValue = "STRING", HelpText = "Path to the exporter config.", Required = true)]
            public string Config { get; set; }
        }

        private class ExportConfig
        {
            public string AssetDirectory = "Assets";
            public string Version = "v0.0.0";
            public string[] Includes = { "*" };
            public string[] Excludes = { };
            public string OutputPath = ".\\Builds\\v0.0.0.unitypackage";
            public string VersionFilename = "version.txt";
        }

        public static void Export()
        {
            PackageExporterArguments args = ParseCommandLineArguments();
            try
            {
                Export(args.Config);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        public static void Export(string configPath)
        {
            if (File.Exists(configPath) == false)
            {
                string msg = string.Format("config in path '{0}' is not found!", configPath);
                throw new ArgumentException(msg);
            }

            ExportConfig config = new ExportConfig();

            try
            {
                string jsonFile = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<ExportConfig>(jsonFile);

                Debug.Log("Config file successfully loaded");
            }
            catch (Exception e)
            {
                Debug.LogWarningFormat("Config file at {0} found, but could not be read. Using default configuration. Exception occuring: '{1}'", configPath, e.GetType().Name);
            }

            if (string.IsNullOrEmpty(config.VersionFilename) == false)
            {
                UpdateVersionFile(config.AssetDirectory + "/" + config.VersionFilename, config.Version);
            }

            // Create the output directory if it doesn't exist yet.
            string outputDirectory = Path.GetDirectoryName(config.OutputPath.Replace('/', '\\'));

            if (string.IsNullOrEmpty(outputDirectory) == false && Directory.Exists(outputDirectory) == false)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] exportedPaths = GetAssetPathsToExport(config);
            Debug.LogFormat("Exporting {0} paths to {1}", exportedPaths.Length, outputDirectory);
            AssetDatabase.ExportPackage(exportedPaths, config.OutputPath.Replace('/', '\\'), ExportPackageOptions.Default);
            Debug.Log("Export completed");
        }

        private static string[] GetAssetPathsToExport(ExportConfig config)
        {
            string root = config.AssetDirectory;

            if (root.Last() != '/')
            {
                root += '/';
            }

            string[] includes = config.Includes.Select(includingPattern => AddRootDirIfNoStartingWildcard(root, includingPattern)).ToArray();
            string[] excludes = config.Excludes.Select(excludingPattern => AddRootDirIfNoStartingWildcard(root, excludingPattern)).ToArray();

            string[] assetPathsInRootDirectory = AssetDatabase.GetAllAssetPaths().Where(assetPath => assetPath.StartsWith(root)).ToArray();
            string[] assetPathsIncludedOnly = assetPathsInRootDirectory.Where(filePath => includes.Any(includingPattern => Regex.IsMatch(filePath, WildcardToRegular(includingPattern)))).ToArray();
            string[] assetPathsWithoutExcluded = assetPathsIncludedOnly.Where(filePath => excludes.Any(excludingPattern => Regex.IsMatch(filePath, WildcardToRegular(excludingPattern))) == false).ToArray();

            return assetPathsWithoutExcluded;
        }

        private static string AddRootDirIfNoStartingWildcard(string rootDirectory, string pattern)
        {
            if (pattern.StartsWith("*") == false)
            {
                return $"{rootDirectory}{pattern}";
            }
            return pattern;
        }

        private static void UpdateVersionFile(string path, string content)
        {
            File.WriteAllText(UnityAssetPathToAbsoluteWindowsPath(path), content);
            AssetDatabase.ImportAsset(path);
            TextAsset versionFile = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (versionFile != null)
            {
                EditorUtility.SetDirty(versionFile);
            }
        }

        private static string UnityAssetPathToAbsoluteWindowsPath(string unityPath)
        {
            // Unity paths always start with "Assets/"
            if (!unityPath.StartsWith("Assets/"))
            {
                throw new Exception("The specified Unity path is not relative to the Project root directory");
            }

            // prepend the path to the unity assets folder
            // replace forward by backward slashes
            return Path.Combine(Application.dataPath.Replace("/", @"\"), unityPath.Substring("Assets/".Length).Replace("/", @"\"));
        }

        private static string WildcardToRegular(string value)
        {
            return "^" + value.Replace("?", ".").Replace("*", ".*") + "$";
        }

        private static PackageExporterArguments ParseCommandLineArguments()
        {
            PackageExporterArguments arguments = new PackageExporterArguments();
            // Redirect Console.Error output to Unity
            Console.SetError(new UnityDebugLogErrorWriter());
            Parser.Default.ParseArguments(Environment.GetCommandLineArgs(), arguments);
            // Unset console output
            Console.SetError(TextWriter.Null);

            return arguments;
        }
    }
}
