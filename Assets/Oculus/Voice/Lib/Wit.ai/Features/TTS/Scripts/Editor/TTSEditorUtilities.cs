/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Data.Configuration;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Editor.Voices;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi;
using Meta.WitAi.Data.Info;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.TTS.Editor
{
    public static class TTSEditorUtilities
    {
        // Default TTS Setup
        public static Transform CreateDefaultSetup()
        {
            // Generate parent
            Transform parent = GenerateGameObject("TTS").transform;

            // Add TTS Service
            TTSService service = CreateService(parent);

            // Add TTS Speaker
            CreateSpeaker(parent, service);

            // Select parent
            Selection.activeObject = parent.gameObject;
            return parent;
        }

        // Default TTS Service
        public static TTSService CreateService(Transform parent = null, bool ignoreErrors = false)
        {
            // Get parent
            if (parent == null)
            {
                Transform selected = Selection.activeTransform;
                if (selected != null && selected.gameObject.scene.rootCount > 0)
                {
                    parent = Selection.activeTransform;
                }
            }
            // Ignore if found
            TTSService instance = GameObject.FindObjectOfType<TTSService>();
            if (instance != null)
            {
                // Log
                if (!ignoreErrors)
                {
                    VLog.W($"TTS Service - A TTSService is already in scene\nGameObject: {instance.gameObject.name}");
                }

                // Move into parent
                if (parent != null)
                {
                    instance.transform.SetParent(parent, true);
                }
            }

            // Generate TTSWit
            else
            {
                instance = CreateWitService(parent);
            }

            // Select & return instance
            Selection.activeObject = instance.gameObject;
            return instance;
        }

        // Default TTS Service
        private static TTSWit CreateWitService(Transform parent = null)
        {
            // Generate new TTSWit & add caches
            TTSWit ttsWit = GenerateGameObject("TTSWitService", parent).AddComponent<TTSWit>();
            ttsWit.gameObject.AddComponent<TTSRuntimeCache>();
            ttsWit.gameObject.AddComponent<TTSDiskCache>();
            VLog.D($"TTS Service - Instantiated Service {ttsWit.gameObject.name}");

            // Refresh configuration
            WitConfiguration configuration = SetupConfiguration(ttsWit);
            if (configuration != null)
            {
                RefreshAvailableVoices(ttsWit);
            }

            // Log
            return ttsWit;
        }

        // Wit configuration
        private static WitConfiguration SetupConfiguration(TTSService instance)
        {
            // Ignore non-tts wit
            if (instance.GetType() != typeof(TTSWit))
            {
                return null;
            }
            // Already setup
            TTSWit ttsWit = instance as TTSWit;
            if (ttsWit.RequestSettings.configuration != null)
            {
                return ttsWit.RequestSettings.configuration;
            }

            // Refresh configuration list
            if (WitConfigurationUtility.WitConfigs == null)
            {
                WitConfigurationUtility.ReloadConfigurationData();
            }

            // Assign first wit configuration found
            if (WitConfigurationUtility.WitConfigs != null && WitConfigurationUtility.WitConfigs.Length > 0)
            {
                ttsWit.RequestSettings.configuration = WitConfigurationUtility.WitConfigs[0];
                VLog.D($"TTS Service - Assigned Wit Configuration {ttsWit.RequestSettings.configuration.name}");
            }

            // Warning
            if (ttsWit.RequestSettings.configuration == null)
            {
                VLog.W($"TTS Service - Please create and assign a WitConfiguration to TTSWit");
            }

            // Return configuration
            return ttsWit.RequestSettings.configuration;
        }

        // Refresh available voices
        private static void RefreshAvailableVoices(TTSWit ttsWit)
        {
            // Fail without configuration
            if (ttsWit == null)
            {
                VLog.W($"TTS Service - Cannot refresh voices without TTS Wit Service");
                return;
            }
            IWitRequestConfiguration configuration = ttsWit.RequestSettings.configuration;
            if (configuration == null)
            {
                VLog.W($"TTS Service - Cannot refresh voices without TTS Wit Configuration");
                return;
            }

            // Get application info
            WitAppInfo appInfo = configuration.GetApplicationInfo();
            if (appInfo.voices == null || appInfo.voices.Length == 0)
            {
                VLog.W($"TTS Service - No voices found");
                if (ttsWit.PresetVoiceSettings == null || ttsWit.PresetVoiceSettings.Length == 0)
                {
                    WitVoiceInfo voiceInfo = new WitVoiceInfo()
                    {
                        name = TTSWitVoiceSettings.DEFAULT_VOICE,
                    };
                    TTSWitVoiceSettings placeholder = GetDefaultVoiceSetting(voiceInfo);
                    ttsWit.SetVoiceSettings(new TTSWitVoiceSettings[] { placeholder });
                }
            }
            // Reset list of voices
            else
            {
                WitVoiceInfo[] voices = appInfo.voices;
                TTSWitVoiceSettings[] newSettings = new TTSWitVoiceSettings[voices.Length];
                for (int i = 0; i < voices.Length; i++)
                {
                    newSettings[i] = GetDefaultVoiceSetting(voices[i]);
                }
                ttsWit.SetVoiceSettings(newSettings);
                VLog.D($"TTS Service - Successfully applied {voices.Length} voices to {ttsWit.gameObject.name}");
            }

            // Refresh
            RefreshEmptySpeakers(ttsWit);
        }

        // Set all blank IDs to default voice id
        private static void RefreshEmptySpeakers(TTSService service)
        {
            string defaultVoiceID = service.VoiceProvider.VoiceDefaultSettings.settingsID;
            foreach (var speaker in GameObject.FindObjectsOfType<TTSSpeaker>())
            {
                if (string.IsNullOrEmpty(speaker.presetVoiceID) || string.Equals(speaker.presetVoiceID, TTSVoiceSettings.DEFAULT_ID))
                {
                    speaker.presetVoiceID = defaultVoiceID;
                }
            }
        }

        // Get default voice settings
        private static TTSWitVoiceSettings GetDefaultVoiceSetting(WitVoiceInfo voiceData)
        {
            TTSWitVoiceSettings result = new TTSWitVoiceSettings()
            {
                settingsID = voiceData.name.ToUpper(),
                voice = voiceData.name
            };
            // Use first style provided
            if (voiceData.styles != null && voiceData.styles.Length > 0)
            {
                result.style = voiceData.styles[0];
            }
            return result;
        }

        // Default TTS Speaker
        public static TTSSpeaker CreateSpeaker(Transform parent = null, TTSService service = null)
        {
            // Get parent
            if (parent == null)
            {
                Transform selected = Selection.activeTransform;
                if (selected != null && selected.gameObject.scene.rootCount > 0)
                {
                    parent = Selection.activeTransform;
                }
            }
            // Generate service if possible
            if (service == null)
            {
                service = CreateService(parent);
            }

            // TTS Speaker
            string goName = typeof(TTSSpeaker).Name;
            TTSSpeaker speaker = GenerateGameObject(goName, parent).AddComponent<TTSSpeaker>();
            speaker.presetVoiceID = string.Empty;

            // Audio Source
            AudioSource audio = GenerateGameObject($"{goName}Audio", speaker.transform).AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f; // Default to 2D
            speaker.AudioSource = audio;

            // Return speaker
            VLog.D($"TTS Service - Instantiated Speaker {speaker.gameObject.name}");
            Selection.activeObject = speaker.gameObject;
            return speaker;
        }

        // Generate with specified name
        private static GameObject GenerateGameObject(string name, Transform parent = null)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent);
            result.localPosition = Vector3.zero;
            result.localRotation = Quaternion.identity;
            result.localScale = Vector3.one;
            return result.gameObject;
        }
    }
}
