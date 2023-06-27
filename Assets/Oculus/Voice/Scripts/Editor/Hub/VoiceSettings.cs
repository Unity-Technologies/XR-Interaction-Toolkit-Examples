/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.Voice.VSDKHub;
using Meta.Voice.Windows;
using Oculus.Voice.Utility;
using UnityEngine;

namespace Meta.Voice.Hub
{
    [MetaHubPage("Settings", VoiceHubConstants.CONTEXT_VOICE, priority: 800)]
    public class SettingsWindowPage : SettingsWindow, IMetaHubPage
    {
        protected override GUIContent Title => new GUIContent("Voice SDK Settings");
        protected override Texture2D HeaderIcon => null;
        protected override string DocsUrl => VoiceSDKStyles.Texts.VoiceDocsUrl;

        public new void OnGUI()
        {
            base.OnGUI();
        }
    }
}
