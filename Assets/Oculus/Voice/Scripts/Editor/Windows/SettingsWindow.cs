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

using Meta.Voice.Hub;
using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.Voice.VSDKHub;
using Meta.WitAi;
using Meta.WitAi.Windows;
using Oculus.Voice.Utility;
using Oculus.Voice.Windows;
using UnityEngine;

namespace Meta.Voice.Windows
{
    public class SettingsWindow : WitWindow
    {
        protected override GUIContent Title => new GUIContent(VoiceSDKStyles.SettingsTitle.text);
        protected override Texture2D HeaderIcon => VoiceSDKStyles.MainHeader;
        protected override string DocsUrl => VoiceSDKStyles.Texts.VoiceDocsUrl;

        protected override void OnEnable()
        {
            WitAuthUtility.tokenValidator = new VoiceSDKTokenValidatorProvider();
            titleContent = Title;
            base.OnEnable();
        }
    }

    [MetaHubPage("Wit Configurations", VoiceHubConstants.CONTEXT_VOICE, priority: 500)]
    public class WitConfigWindowPage : SettingsWindow, IMetaHubPage
    {
        protected override GUIContent Title => new GUIContent("Wit Configurations");
        protected override Texture2D HeaderIcon => null;
        protected override string DocsUrl => VoiceSDKStyles.Texts.VoiceDocsUrl;

        public override bool ShowGeneralSettings => false;

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = Title;
        }

        public new void OnGUI()
        {
            base.OnGUI();
        }

        public override string ToString()
        {
            return Title.text;
        }
    }

    [MetaHubPage("Settings", VoiceHubConstants.CONTEXT_VOICE, priority: 800)]
    public class SettingsWindowPage : SettingsWindow, IMetaHubPage
    {
        protected override GUIContent Title => new GUIContent("Settings");
        protected override Texture2D HeaderIcon => null;
        protected override string DocsUrl => VoiceSDKStyles.Texts.VoiceDocsUrl;

        public override bool ShowWitConfiguration => false;

        protected override void OnEnable()
        {
            base.OnEnable();
            titleContent = Title;
        }

        public new void OnGUI()
        {
            base.OnGUI();
        }
    }
}
