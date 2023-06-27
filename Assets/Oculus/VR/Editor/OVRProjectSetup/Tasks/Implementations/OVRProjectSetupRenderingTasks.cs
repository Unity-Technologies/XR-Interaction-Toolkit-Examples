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
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif


[InitializeOnLoad]
internal static class OVRProjectSetupRenderingTasks
{
#if USING_XR_SDK_OCULUS
    private static Unity.XR.Oculus.OculusSettings OculusSettings
    {
        get
        {
            UnityEditor.EditorBuildSettings.TryGetConfigObject<Unity.XR.Oculus.OculusSettings>(
                "Unity.XR.Oculus.Settings", out var settings);
            return settings;
        }
    }
#endif

#if USING_URP && UNITY_2022_2_OR_NEWER
    // Call action for all UniversalRendererData being used, return true if all the return value of action is true
    private static bool ForEachRendererData(Func<UniversalRendererData, bool> action)
    {
        var ret = true;
        var pipelineAssets = new System.Collections.Generic.List<RenderPipelineAsset>();
        QualitySettings.GetAllRenderPipelineAssetsForPlatform("Android", ref pipelineAssets);
        foreach (var pipelineAsset in pipelineAssets)
        {
            var urpPipelineAsset = pipelineAsset as UniversalRenderPipelineAsset;
            // If using URP pipeline
            if (urpPipelineAsset)
            {
                var path = AssetDatabase.GetAssetPath(urpPipelineAsset);
                var dependency = AssetDatabase.GetDependencies(path);
                for (int i = 0; i < dependency.Length; i++)
                {
                    // Try to read the dependency as UniversalRendererData
                    if (AssetDatabase.GetMainAssetTypeAtPath(dependency[i]) != typeof(UniversalRendererData))
                        continue;

                    UniversalRendererData renderData =
                        (UniversalRendererData)AssetDatabase.LoadAssetAtPath(dependency[i],
                            typeof(UniversalRendererData));
                    if (renderData)
                    {
                        ret = ret && action(renderData);
                    }

                    if (!ret)
                    {
                        break;
                    }
                }
            }
        }

        return ret;
    }
#endif

    private static GraphicsDeviceType[] GetGraphicsAPIs(BuildTargetGroup buildTargetGroup)
    {
        var buildTarget = buildTargetGroup.GetBuildTarget();
        if (PlayerSettings.GetUseDefaultGraphicsAPIs(buildTarget))
        {
            return Array.Empty<GraphicsDeviceType>();
        }

        // Recommends OpenGL ES 3 or Vulkan
        return PlayerSettings.GetGraphicsAPIs(buildTarget);
    }

    static OVRProjectSetupRenderingTasks()
    {
        const OVRProjectSetup.TaskGroup targetGroup = OVRProjectSetup.TaskGroup.Rendering;

        //[Required] Set the color space to linear
        OVRProjectSetup.AddTask(
            conditionalLevel: buildTargetGroup =>
                OVRProjectSetupUtils.IsPackageInstalled(OVRProjectSetupXRTasks.UnityXRPackage)
                    ? OVRProjectSetup.TaskLevel.Required
                    : OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup => PlayerSettings.colorSpace == ColorSpace.Linear,
            message: "Color Space is required to be Linear",
            fix: buildTargetGroup => PlayerSettings.colorSpace = ColorSpace.Linear,
            fixMessage: "PlayerSettings.colorSpace = ColorSpace.Linear"
        );


#if USING_XR_SDK_OCULUS && OCULUS_XR_EYE_TRACKED_FOVEATED_RENDERING && UNITY_2021_3_OR_NEWER
        //[Required] Use Vulkan and IL2CPP/ARM64 when using ETFR
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Required,
            group: targetGroup,
            isDone: buildTargetGroup =>
            {
                var useIL2CPP = PlayerSettings.GetScriptingBackend(buildTargetGroup) == ScriptingImplementation.IL2CPP;
                var useARM64 = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64;
                var useVK = GetGraphicsAPIs(buildTargetGroup).Any(item => item == GraphicsDeviceType.Vulkan);
                return useVK && useARM64 && useIL2CPP;
            },
            message: "Need to use Vulkan for Graphics APIs, IL2CPP for scripting backend, and ARM64 for target architectures when using eye-tracked foveated rendering",
            fix: buildTargetGroup =>
            {
                var buildTarget = buildTargetGroup.GetBuildTarget();
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetGraphicsAPIs(buildTarget, new[] { GraphicsDeviceType.Vulkan });
            },
            fixMessage: "Set target architectures to ARM64, scripting backend to IL2CPP, and Graphics APIs to Vulkan for this build.",
            conditionalValidity: buildTargetGroup => OculusSettings?.FoveatedRenderingMethod == Unity.XR.Oculus.OculusSettings.FoveationMethod.EyeTrackedFoveatedRendering
        );
#endif

        //[Required] Disable Graphics Jobs
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup => !PlayerSettings.graphicsJobs,
            message: "Disable Graphics Jobs",
            fix: buildTargetGroup => PlayerSettings.graphicsJobs = false,
            fixMessage: "PlayerSettings.graphicsJobs = false"
        );

        //[Recommended] Set the Graphics API order for Android
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            platform: BuildTargetGroup.Android,
            group: targetGroup,
            isDone: buildTargetGroup =>
                GetGraphicsAPIs(buildTargetGroup).Any(item =>
                    item == GraphicsDeviceType.OpenGLES3 || item == GraphicsDeviceType.Vulkan),
            message: "Manual selection of Graphic API, favoring Vulkan (or OpenGLES3)",
            fix: buildTargetGroup =>
            {
                var buildTarget = buildTargetGroup.GetBuildTarget();
                PlayerSettings.SetUseDefaultGraphicsAPIs(buildTarget, false);
                PlayerSettings.SetGraphicsAPIs(buildTarget, new[] { GraphicsDeviceType.Vulkan });
            },
            fixMessage: "Set Graphics APIs for this build target to Vulkan"
        );
        //[Required] Set the Graphics API order for Windows
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Required,
            platform: BuildTargetGroup.Standalone,
            group: targetGroup,
            isDone: buildTargetGroup =>
                GetGraphicsAPIs(buildTargetGroup).Any(item =>
                    item == GraphicsDeviceType.Direct3D11),
            message: "Manual selection of Graphic API, favoring Direct3D11",
            fix: buildTargetGroup =>
            {
                var buildTarget = buildTargetGroup.GetBuildTarget();
                PlayerSettings.SetUseDefaultGraphicsAPIs(buildTarget, false);
                PlayerSettings.SetGraphicsAPIs(buildTarget, new[] { GraphicsDeviceType.Direct3D11 });
            },
            fixMessage: "Set Graphics APIs for this build target to Direct3D11"
        );

        //[Recommended] Enable Multithreaded Rendering
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup => PlayerSettings.MTRendering &&
                                        (buildTargetGroup != BuildTargetGroup.Android
                                         || PlayerSettings.GetMobileMTRendering(buildTargetGroup)),
            message: "Enable Multithreaded Rendering",
            fix: buildTargetGroup =>
            {
                PlayerSettings.MTRendering = true;
                if (buildTargetGroup == BuildTargetGroup.Android)
                {
                    PlayerSettings.SetMobileMTRendering(buildTargetGroup, true);
                }
            },
            conditionalFixMessage: buildTargetGroup =>
                buildTargetGroup == BuildTargetGroup.Android
                    ? "PlayerSettings.MTRendering = true and PlayerSettings.SetMobileMTRendering(buildTargetGroup, true)"
                    : "PlayerSettings.MTRendering = true"
        );

#if USING_XR_SDK_OCULUS
        //[Recommended] Select Low Overhead Mode
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            conditionalValidity: buildTargetGroup =>
                GetGraphicsAPIs(buildTargetGroup).Contains(GraphicsDeviceType.OpenGLES3),
            group: targetGroup,
            platform: BuildTargetGroup.Android,
            isDone: buildTargetGroup => OculusSettings?.LowOverheadMode ?? true,
            message: "Use Low Overhead Mode",
            fix: buildTargetGroup =>
            {
                var setting = OculusSettings;
                if (setting != null)
                {
                    setting.LowOverheadMode = true;
                    EditorUtility.SetDirty(setting);
                }
            },
            fixMessage: "OculusSettings.LowOverheadMode = true"
        );

        //[Recommended] Enable Dash Support
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            platform: BuildTargetGroup.Standalone,
            isDone: buildTargetGroup => OculusSettings?.DashSupport ?? true,
            message: "Enable Dash Support",
            fix: buildTargetGroup =>
            {
                var setting = OculusSettings;
                if (setting != null)
                {
                    setting.DashSupport = true;
                    EditorUtility.SetDirty(setting);
                }
            },
            fixMessage: "OculusSettings.DashSupport = true"
        );
#endif

        //[Recommended] Set the Display Buffer Format to 32 bit
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup =>
                PlayerSettings.use32BitDisplayBuffer,
            message: "Use 32Bit Display Buffer",
            fix: buildTargetGroup => PlayerSettings.use32BitDisplayBuffer = true,
            fixMessage: "PlayerSettings.use32BitDisplayBuffer = true"
        );

        //[Recommended] Set the Rendering Path to Forward
        // TODO : Support Scripted Rendering Pipeline?
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup =>
                EditorGraphicsSettings.GetTierSettings(buildTargetGroup, Graphics.activeTier).renderingPath ==
                RenderingPath.Forward,
            message: "Use Forward Rendering Path",
            fix: buildTargetGroup =>
            {
                var renderingTier = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, Graphics.activeTier);
                renderingTier.renderingPath =
                    RenderingPath.Forward;
                EditorGraphicsSettings.SetTierSettings(buildTargetGroup, Graphics.activeTier, renderingTier);
            },
            fixMessage: "renderingTier.renderingPath = RenderingPath.Forward"
        );

        //[Recommended] Set the Stereo Rendering to Instancing
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup =>
                PlayerSettings.stereoRenderingPath == StereoRenderingPath.Instancing,
            message: "Use Stereo Rendering Instancing",
            fix: buildTargetGroup => PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing,
            fixMessage: "PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing"
        );

#if USING_URP && UNITY_2022_2_OR_NEWER
        //[Recommended] When using URP, set Intermediate texture to "Auto"
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup =>
                ForEachRendererData(rd => { return rd.intermediateTextureMode == IntermediateTextureMode.Auto; }),
            message: "Setting the intermate texture mode to \"Always\" might have a performance impact, it is recommended to use \"Auto\"",
            fix: buildTargetGroup =>
                ForEachRendererData(rd => { rd.intermediateTextureMode = IntermediateTextureMode.Auto; return true; }),
            fixMessage: "Set Intermediate texture to \"Auto\""
        );

        //[Recommended] When using URP, disable SSAO
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Recommended,
            group: targetGroup,
            isDone: buildTargetGroup =>
                ForEachRendererData(rd =>
                {
                    return rd.rendererFeatures.Count == 0 || !rd.rendererFeatures.Any(feature => feature.isActive && feature.GetType().Name == "ScreenSpaceAmbientOcclusion");
                }),
            message: "SSAO will have some performace impact, it is recommended to disable SSAO",
            fix: buildTargetGroup =>
                ForEachRendererData(rd =>
                {
                    rd.rendererFeatures.ForEach(feature =>
                        {
                            if (feature.GetType().Name == "ScreenSpaceAmbientOcclusion")
                                feature.SetActive(false);
                        }
                    );
                    return true;
                }),
            fixMessage: "Disable SSAO"
        );
#endif

        //[Optional] Use Non-Directional Lightmaps
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Optional,
            group: targetGroup,
            isDone: buildTargetGroup =>
            {
                return LightmapSettings.lightmaps.Length == 0 ||
                       LightmapSettings.lightmapsMode == LightmapsMode.NonDirectional;
            },
            message: "Use Non-Directional lightmaps",
            fix: buildTargetGroup => LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional,
            fixMessage: "LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional"
        );

        //[Optional] Disable Realtime GI
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Optional,
            group: targetGroup,
            isDone: buildTargetGroup => !Lightmapping.realtimeGI,
            message: "Disable Realtime Global Illumination",
            fix: buildTargetGroup => Lightmapping.realtimeGI = false,
            fixMessage: "Lightmapping.realtimeGI = false"
        );

        //[Optional] GPU Skinning
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Optional,
            platform: BuildTargetGroup.Android,
            group: targetGroup,
            isDone: buildTargetGroup => PlayerSettings.gpuSkinning,
            message: "Consider using GPU Skinning if your application is CPU bound",
            fix: buildTargetGroup => PlayerSettings.gpuSkinning = true,
            fixMessage: "PlayerSettings.gpuSkinning = true"
        );
    }
}
