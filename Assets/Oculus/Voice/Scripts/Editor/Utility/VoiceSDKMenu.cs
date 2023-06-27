/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.Voice.VSDKHub;
using UnityEngine;
using UnityEditor;
using Meta.WitAi.Windows;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Entities;
using Meta.WitAi.TTS.Editor;
using Meta.WitAi.TTS.Editor.Preload;
using Meta.WitAi.Data.Info;
using Oculus.Voice.Windows;

namespace Oculus.Voice.Utility
{
    public static class VoiceSDKMenu
    {
        #region WINDOWS
        [MenuItem("Oculus/Voice SDK/Get Started", false, 1)]
        private static void OpenConfigurationWindow()
        {
            WitWindowUtility.OpenGettingStarted((config) =>
            {
                VoiceSDKHub.ShowPage(VoiceSDKHub.GetPageId(VoiceHubConstants.PAGE_WIT_CONFIGS));
            });
        }
        [MenuItem("Oculus/Voice SDK/Understanding Viewer", false, 200)]
        private static void OpenUnderstandingWindow()
        {
            WitWindowUtility.OpenUnderstandingWindow();
        }
        #endregion

        #region DRAWERS
        [CustomPropertyDrawer(typeof(WitEndpointConfig))]
        public class VoiceCustomEndpointPropertyDrawer : WitEndpointConfigDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitAppInfo))]
        public class VoiceCustomApplicationPropertyDrawer : VoiceApplicationDetailProvider
        {

        }
        [CustomPropertyDrawer(typeof(WitIntentInfo))]
        public class VoiceCustomIntentPropertyDrawer : WitIntentPropertyDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitEntityInfo))]
        public class VoiceCustomEntityPropertyDrawer : WitEntityPropertyDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitTraitInfo))]
        public class VoiceCustomTraitPropertyDrawer : WitTraitPropertyDrawer
        {

        }
        #endregion

        #region Scriptable Objects
        [MenuItem("Assets/Create/Voice SDK/Dynamic Entities")]
        public static void CreateDynamicEntities()
        {
            WitDynamicEntitiesData asset =
                ScriptableObject.CreateInstance<WitDynamicEntitiesData>();

            var path = EditorUtility.SaveFilePanel("Save Dynamic Entity", Application.dataPath,
                "DynamicEntities", "asset");

            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets/" + path.Replace(Application.dataPath, "");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = asset;
            }
        }
        #endregion

        #region TTS

        [MenuItem("Assets/Create/Voice SDK/TTS/Add Default TTS Setup")]
        public static void CreateDefaultTTSSetup()
        {
            TTSEditorUtilities.CreateDefaultSetup();
        }

        [MenuItem("Assets/Create/Voice SDK/TTS/Add TTS Service to Scene", false, 100)]
        public static void CreateTTSService()
        {
            TTSEditorUtilities.CreateService();
        }

        [MenuItem("Assets/Create/Voice SDK/TTS/Add TTS Speaker to Scene", false, 100)]
        public static void CreateTTSSpeaker()
        {
            TTSEditorUtilities.CreateSpeaker();
        }
        [MenuItem("Assets/Create/Voice SDK/TTS/Preload Settings", false, 200)]
        public static void CreateTTSPreloadSettings()
        {
            TTSPreloadUtility.CreatePreloadSettings();
        }
        #endregion
    }
}
