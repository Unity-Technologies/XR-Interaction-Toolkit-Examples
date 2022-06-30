using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

//-------------------------------------------------------------------------------------
// ***** OculusBuildSamples
//
// Provides scripts to build SamplesFramework scene APKs.
//
partial class OculusBuildSamples
{
    private static TimeSpan? minThreshold = TimeSpan.FromSeconds(1);

    static void ImportSamplesFramework() {
        AssetDatabase.ImportPackage("OculusIntegration-release.unitypackage", false);
    }

    static void BuildLocomotion() {
        InitializeBuild("com.oculus.unitysample.locomotion");
        Build("Locomotion");
    }

    static void BuildDistanceGrab() {
        InitializeBuild("com.oculus.unitysample.distancegrab");
        Build("DistanceGrab");
    }

    static void BuildDebugUI() {
        InitializeBuild("com.oculus.unitysample.debugui");
        Build("DebugUI");
    }

    // Note this APK isn't fully functional without some additional
    // build steps listed here:
    // ovrsource/Software/Samples/Unity/SampleFramework/Assets/Oculus/SampleFramework/Usage/Firebase/README.md
    //
    // Here we are only building the smaller (incomplete) build. It's
    // not perfect  but it's better than nothing.
    static void BuildFirebase() {
        InitializeBuild("com.oculus.unitysample.firebase");
        Build("Firebase");
    }

    static void BuildHandsInteractionTrain() {
        InitializeBuild("com.oculus.unitysample.handsinteractiontrain");
        Build("HandsInteractionTrainScene");
    }

    static void BuildMixedRealityCapture() {
        InitializeBuild("com.oculus.unitysample.mixedrealitycapture");
        Build("MixedRealityCapture");
    }

    static void BuildOVROverlay() {
        InitializeBuild("com.oculus.unitysample.ovroverlay");
        Build("OVROverlay");
    }

    static void BuildOVROverlayCanvas() {
        InitializeBuild("com.oculus.unitysample.ovroverlaycanvas");
        Build("OVROverlayCanvas");
    }

    static void BuildStereo180Video() {
        AndroidVideoEditorUtil.EnableNativeVideoPlayer();
        InitializeBuild("com.oculus.unitysample.stereo180video");
        Build("Stereo180Video");
        AndroidVideoEditorUtil.DisableNativeVideoPlayer();
    }

    // TODO(radtker): Add additional build steps for proper functionality.
    static void BuildWidevineVideo() {
        AndroidVideoEditorUtil.EnableNativeVideoPlayer();
        InitializeBuild("com.oculus.unitysample.widevinevideo");
        Build("WidevineVideo");
        AndroidVideoEditorUtil.DisableNativeVideoPlayer();
    }

    // reach out to panya or brittahummel for issues regarding passthrough
    static void BuildPassthrough() {
        InitializeBuild("com.oculus.unitysample.passthrough");
        // TODO: enable OpenXR so Passthrough works
        Build("Passthrough");
    }

    //Reach out to Irad Ratamasky(iradicator) or Rohit Rao (rohitrao) for issues related to enchanced compositor
    static void BuildEnhancedOVROverlay() {
        InitializeBuild("com.oculus.unitysample.enchancedovroverlay");
        BuildInternal("EnhancedOVROverlay"); //Scene is presnet in OculusInternal folder.
    }

    static void BuildStartScene() {
        InitializeBuild("com.oculus.unitysample.startscene");
        Build(
            "StartScene.apk",
            new string[]{
                "Assets/Oculus/SampleFramework/Usage/StartScene.unity",
                "Assets/Oculus/SampleFramework/Usage/Locomotion.unity",
                "Assets/Oculus/SampleFramework/Usage/DistanceGrab.unity",
                "Assets/Oculus/SampleFramework/Usage/DebugUI.unity",
                "Assets/Oculus/SampleFramework/Usage/HandsInteractionTrainScene.unity",
                "Assets/Oculus/SampleFramework/Usage/MixedRealityCapture.unity",
                "Assets/Oculus/SampleFramework/Usage/OVROverlay.unity",
                "Assets/Oculus/SampleFramework/Usage/OVROverlayCanvas.unity",
                "Assets/Oculus/SampleFramework/Usage/Passthrough.unity",
            });
    }

    private static void InitializeBuild(string identifier) {
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.SinglePass;
        GraphicsDeviceType[] graphicsApis = new GraphicsDeviceType[1];
        graphicsApis[0] = GraphicsDeviceType.OpenGLES3;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsApis);
        PlayerSettings.colorSpace = ColorSpace.Linear;
        //Set ARM64 Requirements
        PlayerSettings.SetScriptingBackend (BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture (BuildTargetGroup.Android, 1); //0 - None, 1 - ARM64, 2 - Universal
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        QualitySettings.antiAliasing = 4;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, identifier);
    }

    private static void Build(string sceneName) {
        Build(sceneName + ".apk", new string[] {"Assets/Oculus/SampleFramework/Usage/" + sceneName + ".unity"});
    }

    private static void BuildInternal(string sceneName) {
        Build(sceneName + ".apk", new string[] {"Assets/Oculus/SampleFramework/Usage/OculusInternal/" + sceneName + ".unity"});
    }

    private static void Build(string apkName, string[] scenes) {
          BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
          buildPlayerOptions.target = BuildTarget.Android;
          buildPlayerOptions.locationPathName = apkName;
          buildPlayerOptions.scenes = scenes;
          BuildReport buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
    }
}
