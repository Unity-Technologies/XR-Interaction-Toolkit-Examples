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

using UnityEditor;

[InitializeOnLoad]
internal static class OVRTelemetryPopup
{
    static OVRTelemetryPopup()
    {
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        EditorApplication.update -= Update;
        if (ShouldShowPopup())
        {
            ShowPopup();
        }
    }

    private static bool ShouldShowPopup()
    {
        return !UserHasPreviouslyAnswered();
    }

    private static bool UserHasPreviouslyAnswered()
    {
        return OVRRuntimeSettings.Instance.HasSetTelemetryEnabled;
    }

    private static void ShowPopup()
    {
        var consent = EditorUtility.DisplayDialog(
            "Help improve the Oculus SDKs",
            "Allow Meta to collect usage data on it's SDKs, such as feature and resource usage along with software identifiers such as package name, class names" +
            " and plugin configuration. This data helps improve the Meta SDKs and is collected in accordance with Meta's Privacy Policy." +
            $"\n\nYou can always change this behavior in Edit > Project Settings > {OVRProjectSetupSettingsProvider.SettingsName} > {OVRProjectSettingsProvider.SettingsName} > Telemetry Enabled",
            "Send usage statistics",
            "Don't send");

        RecordConsent(consent);
    }

    private static void RecordConsent(bool consent)
    {
        OVRRuntimeSettings.Instance.TelemetryEnabled = consent;
    }
}
