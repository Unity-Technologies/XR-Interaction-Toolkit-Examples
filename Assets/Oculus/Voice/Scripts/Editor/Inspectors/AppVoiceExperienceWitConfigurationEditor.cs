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

using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Windows;
using Meta.WitAi;
using Meta.WitAi.Data.Info;
using Oculus.Voice.Utility;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Inspectors
{
    [CustomEditor(typeof(WitConfiguration))]
    public class AppVoiceExperienceWitConfigurationEditor : WitConfigurationEditor
    {
        // Override with voice sdk header
        protected override Texture2D HeaderIcon => VoiceSDKStyles.MainHeader;
        public override string HeaderUrl => GetSafeAppUrl(Configuration, WitTexts.WitAppEndpointType.Settings);
        protected override string DocsUrl => VoiceSDKStyles.Texts.VoiceDocsUrl;
        protected override string OpenButtonLabel => IsBuiltInConfiguration(Configuration) ? VoiceSDKStyles.Texts.BuiltInAppBtnLabel : base.OpenButtonLabel;

        // Disable server functionality for built in configurations
        protected override bool _disableServerPost => IsBuiltInConfiguration(Configuration);

        // Use to determine if built in configuration
        public static bool IsBuiltInConfiguration(WitConfiguration witConfiguration)
        {
            if (witConfiguration == null)
            {
                return false;
            }
            return IsBuiltInConfiguration(witConfiguration.GetApplicationInfo());
        }
        public static bool IsBuiltInConfiguration(WitAppInfo appInfo)
        {
            return IsBuiltInConfiguration(appInfo.id);
        }
        public static bool IsBuiltInConfiguration(string applicationID)
        {
            return !string.IsNullOrEmpty(applicationID) && applicationID.StartsWith("voice");
        }

        // Get safe app url
        public static string GetSafeAppUrl(WitConfiguration witConfiguration, WitTexts.WitAppEndpointType endpointType)
        {
            // Use built in app url
            if (IsBuiltInConfiguration(witConfiguration))
            {
                return VoiceSDKStyles.Texts.BuiltInAppUrl;
            }
            // Return wit app id
            return WitTexts.GetAppURL(witConfiguration?.GetApplicationId(), endpointType);
        }
    }
}
