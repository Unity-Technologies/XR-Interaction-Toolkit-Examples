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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class OVRExperimentalCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platformGroup != BuildTargetGroup.Android) return;

        if ((report.summary.options & BuildOptions.AutoRunPlayer) == 0) return;

        const string experimentalEnabledProp = "debug.oculus.experimentalEnabled";

        var config = OVRProjectConfig.GetProjectConfig(false);
        if (!config) return;

        var enabledInProject = config.experimentalFeaturesEnabled;
        if (!enabledInProject) return;

        var adbTool = new OVRADBTool(OVRConfig.Instance.GetAndroidSDKPath());
        if (!adbTool.isReady) return;

        var devices = adbTool.GetDevices();
        if (devices.Count == 0) return;

        var disabledDevices = new List<string>();
        foreach (var device in devices)
        {
            // If we can't read the system prop at all, just ignore
            if (!TryGetSystemProperty(adbTool, device, experimentalEnabledProp, out var expSysPropSet))
                continue;

            var deviceIsExpEnabled = expSysPropSet == 1;
            if (!deviceIsExpEnabled)
            {
                disabledDevices.Add(device);
            }
        }

        if (disabledDevices.Count == 0) return;

        const string menuTitle = "Oculus Experimental";
        var plural = disabledDevices.Count > 1;
        switch (EditorUtility.DisplayDialogComplex(
                    title: menuTitle,
                    message:
                    $"You have enabled experimental features in the project, but the connected device{(plural ? "s" : "")} {string.Join(", ", disabledDevices)} {(plural ? "do" : "does")} not have experimental features enabled ({experimentalEnabledProp}). Would you like to enable them now?",
                    ok: $"Yes, enable experimental features",
                    cancel: "Cancel build",
                    alt: "No, continue build"))
        {
            case 0:
            {
                var failedDevices = new List<string>();
                foreach (var device in disabledDevices)
                {
                    var exitCode = adbTool.RunCommand(new[]
                    {
                        "-s", device,
                        "shell", "setprop", experimentalEnabledProp, "1"
                    }, null, out var stdout, out var stderr);

                    if (exitCode != 0 || !string.IsNullOrEmpty(stdout))
                    {
                        failedDevices.Add(device);
                        Debug.LogError(exitCode != 0
                            ? $"Failed to enable {experimentalEnabledProp} on {device} with error code {exitCode}:\n{stderr}"
                            : $"Failed to enable {experimentalEnabledProp} on {device}:\n{stdout}");
                    }
                    else
                    {
                        Debug.Log($"Successfully set {experimentalEnabledProp} to 1 on {device}.");
                    }
                }

                if (failedDevices.Count > 0)
                {
                    EditorUtility.DisplayDialog(menuTitle,
                        $"Failed to set {experimentalEnabledProp} to 1 on the following devices: {string.Join(", ", failedDevices)}",
                        "Ok");
                }

                break;
            }
            case 1:
            {
                throw new BuildFailedException(
                    $"Build canceled because {experimentalEnabledProp} is not enabled on {string.Join(", ", disabledDevices)}.");
            }
            case 2: break;
        }
    }

    static bool TryGetSystemProperty(OVRADBTool adbTool, string device, string property, string defaultValue,
        out string value)
    {
        if (adbTool.RunCommand(new[]
            {
                "-s", device,
                "shell", "getprop", property
            }, null, out var stdout, out _) != 0)
        {
            value = defaultValue;
            return false;
        }

        stdout = stdout?.Trim();
        value = string.IsNullOrEmpty(stdout) ? defaultValue : stdout;
        return true;
    }

    static bool TryGetSystemProperty(OVRADBTool adbTool, string device, string property, out int value)
    {
        value = 0;
        return TryGetSystemProperty(adbTool, device, property, "0", out var strValue) &&
               int.TryParse(strValue, out value);
    }

}
