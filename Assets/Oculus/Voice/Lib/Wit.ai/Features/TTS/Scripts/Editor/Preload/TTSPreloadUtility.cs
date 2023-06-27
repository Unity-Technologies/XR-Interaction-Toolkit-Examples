/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.TTS.Utilities;
using UnityEngine.SceneManagement;

namespace Meta.WitAi.TTS.Editor.Preload
{
    public static class TTSPreloadUtility
    {
        #region MANAGEMENT
        /// <summary>
        /// Create a new preload settings asset by prompting a save location
        /// </summary>
        public static TTSPreloadSettings CreatePreloadSettings()
        {
            string savePath = WitConfigurationUtility.GetFileSaveDirectory("Save TTS Preload Settings", "TTSPreloadSettings", "asset");
            return CreatePreloadSettings(savePath);
        }
        /// <summary>
        /// Create a new preload settings asset at specified location
        /// </summary>
        public static TTSPreloadSettings CreatePreloadSettings(string savePath)
        {
            // Ignore if empty
            if (string.IsNullOrEmpty(savePath))
            {
                return null;
            }

            // Get asset path
            string assetPath = savePath.Replace("\\", "/");
            if (!assetPath.StartsWith(Application.dataPath))
            {
                VLog.E(
                    $"TTS Preload Utility - Cannot Create Setting Outside of Assets Directory\nPath: {assetPath}");
                return null;
            }
            assetPath = assetPath.Replace(Application.dataPath, "Assets");

            // Generate & save
            TTSPreloadSettings settings = ScriptableObject.CreateInstance<TTSPreloadSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();

            // Reload & return
            return AssetDatabase.LoadAssetAtPath<TTSPreloadSettings>(assetPath);
        }
        /// <summary>
        /// Find all preload settings currently in the Assets directory
        /// </summary>
        public static TTSPreloadSettings[] GetPreloadSettings()
        {
            List<TTSPreloadSettings> results = new List<TTSPreloadSettings>();
            string[] guids = AssetDatabase.FindAssets("t:TTSPreloadSettings");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TTSPreloadSettings settings = AssetDatabase.LoadAssetAtPath<TTSPreloadSettings>(path);
                results.Add(settings);
            }
            return results.ToArray();
        }
        #endregion

        #region ITERATE
        // Performer
        public static CoroutineUtility.CoroutinePerformer _performer;
        //
        public delegate IEnumerator TTSPreloadIterateDelegate(TTSService service, TTSDiskCacheSettings cacheSettings, TTSVoiceSettings voiceSettings, TTSPreloadPhraseData phraseData, Action<float> onProgress, Action<string> onComplete);
        // Iterating
        public static bool IsIterating()
        {
            return _performer != null;
        }
        // Perform a check of all data
        private static bool CheckIterateData(TTSService service, TTSPreloadData preloadData, TTSPreloadIterateDelegate onIterate, Action<float> onProgress, Action<string> onComplete)
        {
            // No service
            if (service == null)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke("\nNo TTSService found in current scene");
                return false;
            }
            // No preload data
            if (preloadData == null)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke("\nTTS Preload Data Not Found");
                return false;
            }
            // No preload data
            if (preloadData.voices == null)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke("\nTTS Preload Data Voices Not Found");
                return false;
            }
            // Ignore if running
            if (Application.isPlaying)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke("Cannot preload while running");
                return false;
            }
            // Ignore if running
            if (onIterate == null)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke("Code recompiled mid update");
                return false;
            }
            return true;
        }
        // Iterate phrases
        private static void IteratePhrases(TTSService service, TTSPreloadData preloadData, TTSPreloadIterateDelegate onIterate, Action<float> onProgress, Action<string> onComplete)
        {
            // Skip if check fails
            if (!CheckIterateData(service, preloadData, onIterate, onProgress, onComplete))
            {
                return;
            }

            // Unload previous coroutine performer
            if (_performer != null)
            {
                _performer.gameObject.DestroySafely();
                _performer = null;
            }

            // Run new coroutine
            _performer = CoroutineUtility.StartCoroutine(PerformIteratePhrases(service, preloadData, onIterate, onProgress, onComplete));
        }
        // Perform iterate
        private static IEnumerator PerformIteratePhrases(TTSService service, TTSPreloadData preloadData, TTSPreloadIterateDelegate onIterate, Action<float> onProgress, Action<string> onComplete)
        {
            // Get cache settings
            TTSDiskCacheSettings cacheSettings = new TTSDiskCacheSettings()
            {
                DiskCacheLocation = TTSDiskCacheLocation.Preload
            };

            // Get total phrases
            int phraseTotal = 0;
            foreach (var voice in preloadData.voices)
            {
                if (voice.phrases == null)
                {
                    continue;
                }
                foreach (var phrase in voice.phrases)
                {
                    phraseTotal++;
                }
            }

            // Begin
            onProgress?.Invoke(0f);

            // Iterate
            int phraseCount = 0;
            float phraseInc = 1f / (float)phraseTotal;
            string log = string.Empty;
            for (int v = 0; v < preloadData.voices.Length; v++)
            {
                // Get voice data
                TTSPreloadVoiceData voiceData = preloadData.voices[v];
                if (voiceData.phrases == null)
                {
                    continue;
                }

                // Get voice
                TTSVoiceSettings voiceSettings = service.GetPresetVoiceSettings(voiceData.presetVoiceID);
                if (voiceSettings == null)
                {
                    log += "\n-Missing Voice Setting: " + voiceData.presetVoiceID;
                    phraseCount += voiceData.phrases.Length;
                    continue;
                }

                // Iterate phrases
                for (int p = 0; p < voiceData.phrases.Length; p++)
                {
                    // Iterate progress
                    float progress = (float) phraseCount / (float) phraseTotal;
                    onProgress?.Invoke(progress);
                    phraseCount++;

                    // Iterate Load
                    yield return onIterate(service, cacheSettings, voiceSettings, voiceData.phrases[p],
                        (p2) => onProgress?.Invoke(progress + p2 * phraseInc), (l) => log += l);

                    // Skip if check fails
                    if (!CheckIterateData(service, preloadData, onIterate, onProgress, onComplete))
                    {
                        yield break;
                    }
                }
            }

            // Complete
            onProgress?.Invoke(1f);
            onComplete?.Invoke(log);
        }
        #endregion

        #region PRELOAD
        // Can preload data
        public static bool CanPreloadData()
        {
            return TTSService.Instance != null;
        }
        // Preload from data
        public static void PreloadData(TTSService service, TTSPreloadData preloadData, Action<float> onProgress, Action<TTSPreloadData, string> onComplete)
        {
            IteratePhrases(service, preloadData, PreloadPhraseData, onProgress, (l) => onComplete?.Invoke(preloadData, l));
        }
        // Preload voice text
        private static IEnumerator PreloadPhraseData(TTSService service, TTSDiskCacheSettings cacheSettings, TTSVoiceSettings voiceSettings, TTSPreloadPhraseData phraseData, Action<float> onProgress, Action<string> onComplete)
        {
            // Begin running
            bool running = true;

            // Download
            string log = string.Empty;
            service.DownloadToDiskCache(phraseData.textToSpeak, string.Empty, voiceSettings, cacheSettings, delegate(TTSClipData data, string path, string error)
            {
                // Set phrase data
                phraseData.clipID = data.clipID;
                phraseData.downloaded = string.IsNullOrEmpty(error);
                // Failed
                if (!phraseData.downloaded)
                {
                    log += $"\n-{voiceSettings.settingsID} Preload Failed: {phraseData.textToSpeak}";
                }
                // Next
                running = false;
            });

            // Wait for running to complete
            while (running)
            {
                //Debug.Log($"Preload Wait: {voiceSettings.settingsID} - {phraseData.textToSpeak}");
                yield return null;
            }

            // Invoke
            onComplete?.Invoke(log);
        }
        #endregion

        #region REFRESH
        // Refresh
        public static void RefreshPreloadData(TTSService service, TTSPreloadData preloadData, Action<float> onProgress, Action<TTSPreloadData, string> onComplete)
        {
            IteratePhrases(service, preloadData, RefreshPhraseData, onProgress, (l) => onComplete?.Invoke(preloadData, l));
        }
        // Refresh
        private static IEnumerator RefreshPhraseData(TTSService service, TTSDiskCacheSettings cacheSettings, TTSVoiceSettings voiceSettings, TTSPreloadPhraseData phraseData, Action<float> onProgress, Action<string> onComplete)
        {
            RefreshPhraseData(service, cacheSettings, voiceSettings, phraseData);
            yield return null;
            onComplete?.Invoke(string.Empty);
        }
        // Refresh phrase data
        public static void RefreshVoiceData(TTSService service, TTSPreloadVoiceData voiceData, TTSDiskCacheSettings cacheSettings, ref string log)
        {
            // Get voice settings
            if (service == null)
            {
                log += "\n-No TTS service found";
                return;
            }
            // No voice data
            if (voiceData == null)
            {
                log += "\n-No voice data provided";
                return;
            }
            // Get voice
            TTSVoiceSettings voiceSettings = service.GetPresetVoiceSettings(voiceData.presetVoiceID);
            if (voiceSettings == null)
            {
                log += "\n-Missing Voice Setting: " + voiceData.presetVoiceID;
                return;
            }
            // Generate
            if (cacheSettings == null)
            {
                cacheSettings = new TTSDiskCacheSettings()
                {
                    DiskCacheLocation = TTSDiskCacheLocation.Preload
                };
            }

            // Iterate phrases
            for (int p = 0; p < voiceData.phrases.Length; p++)
            {
                RefreshPhraseData(service, cacheSettings, voiceSettings, voiceData.phrases[p]);
            }
        }
        // Refresh phrase data
        public static void RefreshPhraseData(TTSService service, TTSDiskCacheSettings cacheSettings, TTSVoiceSettings voiceSettings, TTSPreloadPhraseData phraseData)
        {
            // Get voice settings
            if (service == null || voiceSettings == null || string.IsNullOrEmpty(phraseData.textToSpeak))
            {
                phraseData.clipID = string.Empty;
                phraseData.downloaded = false;
                phraseData.downloadProgress = 0f;
                return;
            }
            if (cacheSettings == null)
            {
                cacheSettings = new TTSDiskCacheSettings()
                {
                    DiskCacheLocation = TTSDiskCacheLocation.Preload
                };
            }

            // Get phrase data
            phraseData.clipID = service.GetClipID(phraseData.textToSpeak, voiceSettings);

            // Check if file exists
            string path = service.GetDiskCachePath(phraseData.textToSpeak, phraseData.clipID, voiceSettings, cacheSettings);
            phraseData.downloaded = File.Exists(path);
            phraseData.downloadProgress = phraseData.downloaded ? 1f : 0f;
        }
        #endregion

        #region DELETE
        // Clear all clips in a tts preload file
        public static void DeleteData(TTSService service)
        {
            // Get test file path
            string path = service.GetDiskCachePath(string.Empty, "TEST", null, new TTSDiskCacheSettings()
            {
                DiskCacheLocation = TTSDiskCacheLocation.Preload
            });
            // Get directory
            string directory = new FileInfo(path).DirectoryName;
            if (!Directory.Exists(directory))
            {
                return;
            }

            // Ask
            if (!EditorUtility.DisplayDialog("Delete Preload Cache",
                $"Are you sure you would like to delete the TTS Preload directory at:\n{directory}?", "Okay", "Cancel"))
            {
                return;
            }

            // Delete recursively
            Directory.Delete(directory, true);
            // Delete meta
            string meta = directory + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }
            // Refresh assets
            AssetDatabase.Refresh();
        }
        #endregion

        #region IMPORT
        /// <summary>
        /// Prompt user for a json file to be imported into an existing TTSPreloadSettings asset
        /// </summary>
        public static bool ImportData(TTSPreloadSettings preloadSettings)
        {
            // Select a file
            string textFilePath = EditorUtility.OpenFilePanel("Select TTS Preload Json", Application.dataPath, "json");
            if (string.IsNullOrEmpty(textFilePath))
            {
                return false;
            }
            // Import with selected file path
            return ImportData(preloadSettings, textFilePath);
        }
        /// <summary>
        /// Imported json data into an existing TTSPreloadSettings asset
        /// </summary>
        public static bool ImportData(TTSPreloadSettings preloadSettings, string textFilePath)
        {
            // Check for file
            if (!File.Exists(textFilePath))
            {
                VLog.E($"TTS Preload Utility - Preload file does not exist\nPath: {textFilePath}");
                return false;
            }
            // Load file
            string textFileContents = File.ReadAllText(textFilePath);
            if (string.IsNullOrEmpty(textFileContents))
            {
                VLog.E($"TTS Preload Utility - Preload file load failed\nPath: {textFilePath}");
                return false;
            }
            // Parse file
            WitResponseNode node = WitResponseNode.Parse(textFileContents);
            if (node == null)
            {
                VLog.E($"TTS Preload Utility - Preload file parse failed\nPath: {textFilePath}");
                return false;
            }
            // Iterate children for texts
            WitResponseClass data = node.AsObject;
            Dictionary<string, List<string>> textsByVoice = new Dictionary<string, List<string>>();
            foreach (var voiceName in data.ChildNodeNames)
            {
                // Get texts list
                List<string> texts;
                if (textsByVoice.ContainsKey(voiceName))
                {
                    texts = textsByVoice[voiceName];
                }
                else
                {
                    texts = new List<string>();
                }

                // Add text phrases
                string[] voicePhrases = data[voiceName].AsStringArray;
                if (voicePhrases != null)
                {
                    foreach (var phrase in voicePhrases)
                    {
                        if (!string.IsNullOrEmpty(phrase) && !texts.Contains(phrase))
                        {
                            texts.Add(phrase);
                        }
                    }
                }

                // Apply
                textsByVoice[voiceName] = texts;
            }
            // Import
            return ImportData(preloadSettings, textsByVoice);
        }
        /// <summary>
        /// Find all ITTSPhraseProviders loaded in scenes & generate
        /// data file to import all phrases associated with the files.
        /// </summary>
        public static bool ImportPhrases(TTSPreloadSettings preloadSettings)
        {
            // Find phrase providers in all scenes
            List<ITTSPhraseProvider> phraseProviders = new List<ITTSPhraseProvider>();
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                foreach (var root in scene.GetRootGameObjects())
                {
                    ITTSPhraseProvider[] found = root.GetComponentsInChildren<ITTSPhraseProvider>(true);
                    if (found != null)
                    {
                        phraseProviders.AddRange(found);
                    }
                }
            }
            // Get all phrases by voice id
            Dictionary<string, List<string>> textsByVoice = new Dictionary<string, List<string>>();
            foreach (var phraseProvider in phraseProviders)
            {
                // Ignore if no voices are found
                string[] voiceIds = phraseProvider.GetVoiceIds();
                if (voiceIds == null || voiceIds.Length == 0)
                {
                    continue;
                }

                // Iterate voice ids
                foreach (var voiceId in voiceIds)
                {
                    // Ignore empty voice id
                    if (string.IsNullOrEmpty(voiceId))
                    {
                        continue;
                    }

                    // Ignore if phrases are null
                    string[] phrases = phraseProvider.GetVoicePhrases(voiceId);
                    if (phrases == null || phrases.Length == 0)
                    {
                        continue;
                    }

                    // Get phrase list
                    List<string> voicePhrases;
                    if (textsByVoice.ContainsKey(voiceId))
                    {
                        voicePhrases = textsByVoice[voiceId];
                    }
                    else
                    {
                        voicePhrases = new List<string>();
                    }

                    // Append unique phrases
                    foreach (var phrase in phrases)
                    {
                        if (!string.IsNullOrEmpty(phrase) && !voicePhrases.Contains(phrase))
                        {
                            voicePhrases.Add(phrase);
                        }
                    }

                    // Apply phrase list
                    textsByVoice[voiceId] = voicePhrases;
                }
            }
            // Import with data
            return ImportData(preloadSettings, textsByVoice);
        }
        /// <summary>
        /// Imported dictionary data into an existing TTSPreloadSettings asset
        /// </summary>
        public static bool ImportData(TTSPreloadSettings preloadSettings, Dictionary<string, List<string>> textsByVoice)
        {
            // Import
            if (preloadSettings == null)
            {
                VLog.E("TTS Preload Utility - Import Failed - Null Preload Settings");
                return false;
            }

            // Whether or not changed
            bool changed = false;

            // Generate if needed
            if (preloadSettings.data == null)
            {
                preloadSettings.data = new TTSPreloadData();
                changed = true;
            }

            // Begin voice list
            List<TTSPreloadVoiceData> voices = new List<TTSPreloadVoiceData>();
            if (preloadSettings.data.voices != null)
            {
                voices.AddRange(preloadSettings.data.voices);
            }

            // Iterate voice names
            foreach (var voiceName in textsByVoice.Keys)
            {
                // Get voice index if possible
                int voiceIndex = voices.FindIndex((v) => string.Equals(v.presetVoiceID, voiceName));

                // Generate voice
                TTSPreloadVoiceData voice;
                if (voiceIndex == -1)
                {
                    voice = new TTSPreloadVoiceData();
                    voice.presetVoiceID = voiceName;
                    voiceIndex = voices.Count;
                    voices.Add(voice);
                }
                // Use existing
                else
                {
                    voice = voices[voiceIndex];
                }

                // Get texts & phrases for current voice
                List<string> texts = new List<string>();
                List<TTSPreloadPhraseData> phrases = new List<TTSPreloadPhraseData>();
                if (voice.phrases != null)
                {
                    foreach (var phrase in voice.phrases)
                    {
                        if (!string.IsNullOrEmpty(phrase.textToSpeak) && !texts.Contains(phrase.textToSpeak))
                        {
                            texts.Add(phrase.textToSpeak);
                            phrases.Add(phrase);
                        }
                    }
                }

                // Get data
                List<string> newTexts = textsByVoice[voiceName];
                if (newTexts != null)
                {
                    foreach (var newText in newTexts)
                    {
                        if (!string.IsNullOrEmpty(newText) && !texts.Contains(newText))
                        {
                            changed = true;
                            texts.Add(newText);
                            phrases.Add(new TTSPreloadPhraseData()
                            {
                                textToSpeak = newText
                            });
                        }
                    }
                }

                // Apply voice
                voice.phrases = phrases.ToArray();
                voices[voiceIndex] = voice;
            }

            // Apply data
            if (changed)
            {
                preloadSettings.data.voices = voices.ToArray();
                EditorUtility.SetDirty(preloadSettings);
            }

            // Return changed
            return changed;
        }
        #endregion
    }
}
