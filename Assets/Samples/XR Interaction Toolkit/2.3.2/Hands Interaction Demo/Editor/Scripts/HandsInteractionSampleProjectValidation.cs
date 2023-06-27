using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Samples.Hands
{
    /// <summary>
    /// Unity Editor class which registers Project Validation rules for the Hands Interaction Demo sample,
    /// checking that other required samples and packages are installed.
    /// </summary>
    static class HandsInteractionSampleProjectValidation
    {
        const string k_SampleDisplayName = "Hands Interaction Demo";
        const string k_Category = "XR Interaction Toolkit";
        const string k_StarterAssetsSampleName = "Starter Assets";
        const string k_HandVisualizerSampleName = "HandVisualizer";

        static readonly BuildTargetGroup[] s_BuildTargetGroups =
            ((BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup))).Distinct().ToArray();

        static readonly List<BuildValidationRule> s_BuildValidationRules = new List<BuildValidationRule>
        {
            new BuildValidationRule
            {
                IsRuleEnabled = () => s_HandsPackageAddRequest == null || s_HandsPackageAddRequest.IsCompleted,
                Message = $"[{k_SampleDisplayName}] XR Hands (com.unity.xr.hands) package must be installed to use this sample.",
                Category = k_Category,
                CheckPredicate = () => PackageVersionUtility.GetPackageVersion("com.unity.xr.hands") >= new PackageVersion("1.1.0"),
                FixIt = () =>
                {
                    s_HandsPackageAddRequest = Client.Add("com.unity.xr.hands");
                    if (s_HandsPackageAddRequest.Error != null)
                    {
                        Debug.LogError($"Package installation error: {s_HandsPackageAddRequest.Error}: {s_HandsPackageAddRequest.Error.message}");
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
            new BuildValidationRule
            {
                IsRuleEnabled = () => PackageVersionUtility.GetPackageVersion("com.unity.xr.hands") >= new PackageVersion("1.1.0"),
                Message = $"[{k_SampleDisplayName}] {k_HandVisualizerSampleName} sample from XR Hands (com.unity.xr.hands) package must be imported or updated to use this sample.",
                Category = k_Category,
                CheckPredicate = () => TryFindSample("com.unity.xr.hands", string.Empty, k_HandVisualizerSampleName, out var sample) && sample.isImported,
                FixIt = () =>
                {
                    if (TryFindSample("com.unity.xr.hands", string.Empty, k_HandVisualizerSampleName, out var sample))
                    {
                        sample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
            new BuildValidationRule
            {
                Message = $"[{k_SampleDisplayName}] {k_StarterAssetsSampleName} sample from XR Interaction Toolkit (com.unity.xr.interaction.toolkit) package must be imported or updated to use this sample.",
                Category = k_Category,
                CheckPredicate = () => TryFindSample("com.unity.xr.interaction.toolkit", string.Empty, k_StarterAssetsSampleName, out var sample) && sample.isImported,
                FixIt = () =>
                {
                    if (TryFindSample("com.unity.xr.interaction.toolkit", string.Empty, k_StarterAssetsSampleName, out var sample))
                    {
                        sample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
        };

        static AddRequest s_HandsPackageAddRequest;

        [InitializeOnLoadMethod]
        static void RegisterProjectValidationRules()
        {
            foreach (var buildTargetGroup in s_BuildTargetGroups)
            {
                BuildValidator.AddRules(buildTargetGroup, s_BuildValidationRules);
            }
        }

        static bool TryFindSample(string packageName, string packageVersion, string sampleDisplayName, out Sample sample)
        {
            sample = default;

            var packageSamples = Sample.FindByPackage(packageName, packageVersion);
            if (packageSamples == null)
            {
                Debug.LogError($"Couldn't find samples of the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
                return false;
            }

            foreach (var packageSample in packageSamples)
            {
                if (packageSample.displayName == sampleDisplayName)
                {
                    sample = packageSample;
                    return true;
                }
            }

            Debug.LogError($"Couldn't find {sampleDisplayName} sample in the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
            return false;
        }

        static string ToString(string packageName, string packageVersion)
        {
            return string.IsNullOrEmpty(packageVersion) ? packageName : $"{packageName}@{packageVersion}";
        }
    }
}
