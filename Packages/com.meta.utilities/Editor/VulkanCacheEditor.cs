// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Meta.Utilities.Editor
{
    public static class VulkanCacheEditor
    {
        public static async void SaveVulkanCache(string suffix)
        {
            var packageName = PlayerSettings.applicationIdentifier;
            var androidFilePath = $"/sdcard/Android/data/{packageName}/cache/vulkan_pso_cache.bin";
            var targetCachePath = $"{Application.dataPath}/psocache.androidlib/assets/vulkan_pso_cache_{suffix}.bin";
            var adbCommand = $"adb pull {androidFilePath} {targetCachePath}";
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {adbCommand}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            _ = process.Start();
            process.WaitForExit();

            var processMessages = $"\nADB Output: {await process.StandardOutput.ReadToEndAsync()}\nADB Error: {await process.StandardError.ReadToEndAsync()}";

            if (File.Exists(targetCachePath))
            {
                Debug.Log($"Vulkan PSO Cache successfully saved to: {targetCachePath}\n{processMessages}");
            }
            else
            {
                Debug.LogError($"Failed to save Vulkan PSO Cache to: {targetCachePath}\n{processMessages}");
            }
        }

        [MenuItem("Tools/PSO Cache/Save Vulkan PSO Cache (Quest 2)", priority = BuildTools.MenuPriority)]
        public static void SaveVulkanCache_Quest2()
        {
            SaveVulkanCache("quest2");
        }

        [MenuItem("Tools/PSO Cache/Save Vulkan PSO Cache (Quest 3)", priority = BuildTools.MenuPriority)]
        public static void SaveVulkanCache_Quest3()
        {
            SaveVulkanCache("quest3");
        }
    }
}
