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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class OVRMovementTool
{
    private const string k_SetupCharacterForBodyTrackingMovementToolsMenuStr =
        "GameObject/Movement/Setup Character for Body Tracking/";

    private const string oculusSkeletonFormat = "Format: Oculus Skeleton";

    [MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr + oculusSkeletonFormat, true)]
    private static bool ValidateSetupCharacterForOculusSkeletonBodyTracking()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr + oculusSkeletonFormat)]
    private static void SetupCharacterForOculusSkeletonBodyTracking()
    {
        SetUpCharacterForBodyTracking(OVRCustomSkeleton.RetargetingType.OculusSkeleton);
    }

    private static void SetUpCharacterForBodyTracking(OVRCustomSkeleton.RetargetingType retargetingType)
    {
        Undo.IncrementCurrentGroup();
        var gameObject = Selection.activeGameObject;

        var body = gameObject.GetComponentInParent<OVRBody>();
        if (!body)
        {
            body = gameObject.AddComponent<OVRBody>();
            Undo.RegisterCreatedObjectUndo(body, "Create OVRBody component");
        }

        var skeleton = gameObject.GetComponent<OVRCustomSkeleton>();
        if (!skeleton)
        {
            skeleton = gameObject.AddComponent<OVRCustomSkeleton>();
            Undo.RegisterCreatedObjectUndo(skeleton, "Create OVRCustomSkeleton component");
        }

        Undo.RegisterFullObjectHierarchyUndo(skeleton, "Auto-map OVRCustomSkeleton bones");
        skeleton.SetSkeletonType(OVRSkeleton.SkeletonType.Body);
        skeleton.retargetingType = retargetingType;

        skeleton.AutoMapBones(retargetingType);
        EditorUtility.SetDirty(skeleton);
        EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);

        var projectConfig = OVRProjectConfig.CachedProjectConfig;
        projectConfig.bodyTrackingSupport = OVRProjectConfig.FeatureSupport.Supported;
        OVRProjectConfig.CommitProjectConfig(projectConfig);

        var ovrManager = OVRProjectSetupUtils.FindComponentInScene<OVRManager>();
        if (ovrManager != null)
        {
            ovrManager.requestBodyTrackingPermissionOnStartup = true;
            EditorUtility.SetDirty(ovrManager);
        }

        Undo.SetCurrentGroupName($"Setup Character for {retargetingType} Body Tracking");
    }

    private const string unityHumanoidFormat = "Format: Unity Humanoid";

    [MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr + unityHumanoidFormat, true)]
    private static bool ValidateSetupCharacterForUnityHumanoidBodyTracking()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr + unityHumanoidFormat)]
    private static void SetupCharacterForUnityHumanoidBodyTracking()
    {
        try
        {
            OVRUnityHumanoidSkeletonRetargeter
                .ValidateGameObjectForUnityHumanoidRetargeting(Selection.activeGameObject);
        }
        catch (InvalidOperationException e)
        {
            EditorUtility.DisplayDialog("Character Setup Error", e.Message, "Ok");
            return;
        }

        SetUpCharacterForUnityHumanoidRetargeting();
    }

    private static void SetUpCharacterForUnityHumanoidRetargeting()
    {
        Undo.IncrementCurrentGroup();
        var gameObject = Selection.activeGameObject;

        var body = gameObject.GetComponent<OVRBody>();
        if (!body)
        {
            body = gameObject.AddComponent<OVRBody>();
            Undo.RegisterCreatedObjectUndo(body, "Create OVRBody component");
        }

        var skeleton = gameObject.GetComponent<OVRUnityHumanoidSkeletonRetargeter>();
        if (!skeleton)
        {
            skeleton = gameObject.AddComponent<OVRUnityHumanoidSkeletonRetargeter>();
            Undo.RegisterCreatedObjectUndo(skeleton, "Create OVRCustomSkeleton component");
        }

        Undo.RegisterFullObjectHierarchyUndo(skeleton, "Auto-map OVRCustomSkeleton bones");

        EditorUtility.SetDirty(skeleton);
        EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);

        Undo.SetCurrentGroupName($"Setup Character for Unity Humanoid Retargeting");
    }

    private const string k_SetupCharacterForFaceTrackingMovementToolsMenuStr =
        "GameObject/Movement/Setup Character for Face Tracking/";

    private const string oculusFaceFormat = "Format: Oculus Face";

    [MenuItem(k_SetupCharacterForFaceTrackingMovementToolsMenuStr + oculusFaceFormat, true)]
    private static bool ValidateSetupCharacterForOculusFaceTracking()
    {
        return Selection.activeGameObject != null;
    }

    [MenuItem(k_SetupCharacterForFaceTrackingMovementToolsMenuStr + oculusFaceFormat)]
    private static void SetupCharacterForOculusFaceTracking()
    {
        try
        {
            ValidateGameObjectFaceRetargeting(Selection.activeGameObject);
        }
        catch (InvalidOperationException e)
        {
            EditorUtility.DisplayDialog("Character Setup Error", e.Message, "Ok");
            return;
        }

        SetUpCharacterForFaceTracking(OVRCustomFace.RetargetingType.OculusFace);
    }

    public static void ValidateGameObjectFaceRetargeting(GameObject go)
    {
        var renderer = go.GetComponent<SkinnedMeshRenderer>();
        if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
        {
            throw new InvalidOperationException(
                $"Retargeting to Oculus Face requires an {nameof(SkinnedMeshRenderer)} component with a mesh that contains blendshapes.");
        }
    }


    private static void SetUpCharacterForFaceTracking(OVRCustomFace.RetargetingType retargetingType)
    {
        Undo.IncrementCurrentGroup();
        var gameObject = Selection.activeGameObject;

        var faceExpressions = gameObject.GetComponentInParent<OVRFaceExpressions>();
        if (!faceExpressions)
        {
            faceExpressions = gameObject.AddComponent<OVRFaceExpressions>();
            Undo.RegisterCreatedObjectUndo(faceExpressions, "Create OVRFaceExpressions component");
        }

        var face = gameObject.GetComponent<OVRCustomFace>();
        if (!face)
        {
            face = gameObject.AddComponent<OVRCustomFace>();
            face._faceExpressions = faceExpressions;
            Undo.RegisterCreatedObjectUndo(face, "Create OVRCustomFace component");
        }

        Undo.RegisterFullObjectHierarchyUndo(face, "Auto-map OVRCustomFace blendshapes");

        face.retargetingType = retargetingType;
        face.AutoMapBlendshapes();
        EditorUtility.SetDirty(face);
        EditorSceneManager.MarkSceneDirty(face.gameObject.scene);

        var projectConfig = OVRProjectConfig.CachedProjectConfig;
        projectConfig.faceTrackingSupport = OVRProjectConfig.FeatureSupport.Supported;
        OVRProjectConfig.CommitProjectConfig(projectConfig);

        var ovrManager = OVRProjectSetupUtils.FindComponentInScene<OVRManager>();
        if (ovrManager != null)
        {
            ovrManager.requestFaceTrackingPermissionOnStartup = true;
            EditorUtility.SetDirty(ovrManager);
        }

        Undo.SetCurrentGroupName($"Setup Character for {retargetingType} Face Tracking");
    }
}
