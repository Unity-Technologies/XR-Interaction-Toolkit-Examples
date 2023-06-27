/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Dictation.Data
{
    public class AppDictationDataCreation
    {
        [MenuItem("Assets/Create/Voice SDK/Add App Dictation Experience to Scene", false, 100)]
        public static void AddVoiceCommandServiceToScene()
        {
            var witGo = new GameObject();
            witGo.name = "App Dictation Experience";
            var wit = witGo.AddComponent<AppDictationExperience>();
            wit.RuntimeDictationConfiguration = new WitDictationRuntimeConfiguration
            {
                witConfiguration = WitDataCreation.FindDefaultWitConfig()
            };
        }
    }
}
