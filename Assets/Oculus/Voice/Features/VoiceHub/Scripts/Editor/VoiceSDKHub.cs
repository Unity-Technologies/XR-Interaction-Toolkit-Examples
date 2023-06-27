/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub;
using Meta.Voice.Hub.Attributes;
using UnityEditor;

namespace Meta.Voice.VSDKHub
{
    [MetaHubContext(VoiceHubConstants.CONTEXT_VOICE)]
    public class VoiceSDKHub : MetaHubContext
    {
        [MenuItem("Oculus/Voice SDK/Voice Hub", false, 1)]
        private static void ShowWindow()
        {
            MetaHub.ShowWindow<MetaHub>(VoiceHubConstants.CONTEXT_VOICE);
        }

        public static void ShowPage(string page)
        {
            var window = MetaHub.ShowWindow<MetaHub>(VoiceHubConstants.CONTEXT_VOICE);
            window.SelectedPage = page;
        }

        public static string GetPageId(string pageName)
        {
            return VoiceHubConstants.CONTEXT_VOICE + "::" + pageName;
        }
    }
}
