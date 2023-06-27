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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class OVRProjectSettingsProvider : SettingsProvider
{
    public const string SettingsName = "Settings";

    private OVRProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
        : base(path, scopes, keywords)
    {
    }

    [SettingsProvider]
    public static SettingsProvider CreateProjectValidationSettingsProvider()
    {
        return new OVRProjectSettingsProvider($"{OVRProjectSetupSettingsProvider.SettingsPath}/{SettingsName}",
            SettingsScope.Project);
    }

    public override void OnGUI(string searchContext)
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            var telemetryEnabled = EditorGUILayout.Toggle(new GUIContent("Enable Telemetry"),
                OVRRuntimeSettings.Instance.TelemetryEnabled);

            if (check.changed)
            {
                OVRRuntimeSettings.Instance.TelemetryEnabled = telemetryEnabled;
            }
        }
    }
}
