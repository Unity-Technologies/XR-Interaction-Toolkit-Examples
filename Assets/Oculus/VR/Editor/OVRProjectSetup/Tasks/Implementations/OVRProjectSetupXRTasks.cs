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
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if USING_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif

#if USING_XR_SDK_OCULUS
using Unity.XR.Oculus;
#endif

[InitializeOnLoad]
internal static class OVRProjectSetupXRTasks
{
    internal const string OculusXRPackageName = "com.unity.xr.oculus";
    internal const string XRPluginManagementPackageName = "com.unity.xr.management";
    internal const string UnityXRPackage = "com.unity.xr.openxr";

    private const OVRProjectSetup.TaskGroup XRTaskGroup = OVRProjectSetup.TaskGroup.Packages;

    static OVRProjectSetupXRTasks()
    {
        OVRProjectSetup.AddTask(
            conditionalValidity: buildTargetGroup => OVRProjectSetupUtils.PackageManagerListAvailable,
            level: OVRProjectSetup.TaskLevel.Required,
            group: XRTaskGroup,
            isDone: buildTargetGroup => OVRProjectSetupUtils.IsPackageInstalled(OculusXRPackageName),
            message: "The Oculus XR Plug-in package must be installed",
            fix: buildTargetGroup => OVRProjectSetupUtils.InstallPackage(OculusXRPackageName),
            fixMessage: $"Install {OculusXRPackageName} package"
        );

        OVRProjectSetup.AddTask(
            conditionalValidity: buildTargetGroup => OVRProjectSetupUtils.PackageManagerListAvailable,
            level: OVRProjectSetup.TaskLevel.Required,
            group: XRTaskGroup,
            isDone: buildTargetGroup => OVRProjectSetupUtils.IsPackageInstalled(XRPluginManagementPackageName),
            message: "The XR Plug-in Management package must be installed",
            fix: buildTargetGroup => OVRProjectSetupUtils.InstallPackage(XRPluginManagementPackageName),
            fixMessage: $"Install {XRPluginManagementPackageName} package"
        );

        OVRProjectSetup.AddTask(
            conditionalValidity: buildTargetGroup => OVRProjectSetupUtils.PackageManagerListAvailable,
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: XRTaskGroup,
            isDone: buildTargetGroup => !OVRProjectSetupUtils.IsPackageInstalled(UnityXRPackage),
            message: "Unity's OpenXR Plugin is not recommended when using the Oculus SDK, " +
                     "please use Oculus XR Plug-in instead",
            fix: buildTargetGroup => OVRProjectSetupUtils.UninstallPackage(UnityXRPackage),
            fixMessage: $"Remove the {UnityXRPackage} package"
        );

        AddXrPluginManagementTasks();
    }

    private static void AddXrPluginManagementTasks()
    {
#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS
        OVRProjectSetup.AddTask(
            conditionalValidity: buildTargetGroup =>
                OVRProjectSetupUtils.IsPackageInstalled(XRPluginManagementPackageName),
            level: OVRProjectSetup.TaskLevel.Required,
            group: XRTaskGroup,
            isDone: buildTargetGroup =>
            {
                var settings = GetXRGeneralSettingsForBuildTarget(buildTargetGroup, false);
                if (settings == null)
                {
                    return false;
                }

                foreach (var loader in settings.Manager.activeLoaders)
                {
                    if (loader as OculusLoader != null)
                    {
                        return true;
                    }
                }

                return false;
            },
            message: "Oculus must be added to the XR Plugin active loaders",
            fix: buildTargetGroup =>
            {
                var settings = GetXRGeneralSettingsForBuildTarget(buildTargetGroup, true);
                if (settings == null)
                {
                    throw new OVRConfigurationTaskException("Could not find XR Plugin Manager settings");
                }

                var loadersList = AssetDatabase.FindAssets($"t: {nameof(OculusLoader)}")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<OculusLoader>).ToList();
                OculusLoader oculusLoader;
                if (loadersList.Count > 0)
                {
                    oculusLoader = loadersList[0];
                }
                else
                {
                    oculusLoader = ScriptableObject.CreateInstance<OculusLoader>();
                    AssetDatabase.CreateAsset(oculusLoader, "Assets/XR/Loaders/Oculus Loader.asset");
                }

                settings.Manager.TryAddLoader(oculusLoader);
            },
            fixMessage: "Add Oculus to the XR Plugin active loaders"
        );
#endif
    }

#if USING_XR_MANAGEMENT && USING_XR_SDK_OCULUS
    private static UnityEngine.XR.Management.XRGeneralSettings GetXRGeneralSettingsForBuildTarget(
        BuildTargetGroup buildTargetGroup, bool create)
    {
        var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
        if (!create || settings != null)
        {
            return settings;
        }

        // we have to create these settings ourselves as
        // long as Unity doesn't expose the internal function
        // XRGeneralSettingsPerBuildTarget.GetOrCreate()
        var settingsKey = UnityEngine.XR.Management.XRGeneralSettings.k_SettingsKey;
        EditorBuildSettings.TryGetConfigObject<XRGeneralSettingsPerBuildTarget>(
            settingsKey, out var settingsPerBuildTarget);

        if (settingsPerBuildTarget == null)
        {
            settingsPerBuildTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
                AssetDatabase.CreateFolder("Assets", "XR");
            const string assetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
            AssetDatabase.CreateAsset(settingsPerBuildTarget, assetPath);
            AssetDatabase.SaveAssets();

            EditorBuildSettings.AddConfigObject(settingsKey, settingsPerBuildTarget, true);
        }

        if (!settingsPerBuildTarget.HasManagerSettingsForBuildTarget(buildTargetGroup))
        {
            settingsPerBuildTarget.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
        }

        return XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
    }
#endif
}
