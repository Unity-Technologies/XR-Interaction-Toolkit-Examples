using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Samples.MetaGazeAdapter
{
    /// <summary>
    /// Unity Editor class which registers Project Validation rules for the Meta Gaze Adapter sample,
    /// checking that other required packages are installed.
    /// </summary>
    static class GazeAdapterSampleProjectValidation
    {
        const string k_SampleDisplayName = "Meta Gaze Adapter";
        const string k_Category = "XR Interaction Toolkit";

        static readonly BuildTargetGroup[] s_BuildTargetGroups =
            ((BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup))).Distinct().ToArray();

        static readonly List<BuildValidationRule> s_BuildValidationRules = new List<BuildValidationRule>
        {
            new BuildValidationRule
            {
                IsRuleEnabled = () => s_OpenXRPackageAddRequest == null || s_OpenXRPackageAddRequest.IsCompleted,
                Message = $"[{k_SampleDisplayName}] OpenXR Plugin (com.unity.xr.openxr) package version 1.6.0 or newer must be installed to use this sample.",
                Category = k_Category,
                CheckPredicate = () => PackageVersionUtility.GetPackageVersion("com.unity.xr.openxr").ToMajorMinorPatch() >= new PackageVersion("1.6.0"),
                FixIt = () =>
                {
                    s_OpenXRPackageAddRequest = Client.Add("com.unity.xr.openxr@1.6.0");
                    if (s_OpenXRPackageAddRequest.Error != null)
                    {
                        Debug.LogError($"Package installation error: {s_OpenXRPackageAddRequest.Error}: {s_OpenXRPackageAddRequest.Error.message}");
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
            new BuildValidationRule
            {
                IsRuleEnabled = () => s_OculusPackageAddRequest == null || s_OculusPackageAddRequest.IsCompleted,
                Message = $"[{k_SampleDisplayName}] Oculus XR Plugin (com.unity.xr.oculus) package version 3.2.2 or newer must be installed to use this sample.",
                Category = k_Category,
                CheckPredicate = () => PackageVersionUtility.GetPackageVersion("com.unity.xr.oculus").ToMajorMinorPatch() >= new PackageVersion("3.2.2"),
#if UNITY_2021_3_OR_NEWER
                FixIt = () =>
                {
                    s_OculusPackageAddRequest = Client.Add("com.unity.xr.oculus@3.2.2");
                    if (s_OculusPackageAddRequest.Error != null)
                    {
                        Debug.LogError($"Package installation error: {s_OculusPackageAddRequest.Error}: {s_OculusPackageAddRequest.Error.message}");
                    }
                },
                FixItAutomatic = true,
#else
                FixItAutomatic = false,
#endif
                Error = true,
                HelpText = "This version of the Oculus XR Plugin requires at least Unity 2021.3.4f1",
            },
        };

        static AddRequest s_OpenXRPackageAddRequest;
        static AddRequest s_OculusPackageAddRequest;

        [InitializeOnLoadMethod]
        static void RegisterProjectValidationRules()
        {
            foreach (var buildTargetGroup in s_BuildTargetGroups)
            {
                BuildValidator.AddRules(buildTargetGroup, s_BuildValidationRules);
            }
        }
    }
}
