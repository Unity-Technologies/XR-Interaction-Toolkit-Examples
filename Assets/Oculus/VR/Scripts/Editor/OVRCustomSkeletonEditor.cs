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
using System.ComponentModel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(OVRCustomSkeleton))]
public class OVRCustomSkeletonEditor : OVRSkeletonEditor
{
    public override void OnInspectorGUI()
    {
        var skeleton = (OVRCustomSkeleton)target;

        base.OnInspectorGUI();


        DrawBonesMapping(skeleton);
    }

    private static void DrawBonesMapping(OVRCustomSkeleton skeleton)
    {
        if (GUILayout.Button($"Auto Map Bones"))
        {
            skeleton.AutoMapBones(skeleton.retargetingType);
            EditorUtility.SetDirty(skeleton);
            EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);
        }

        EditorGUILayout.LabelField("Bones", EditorStyles.boldLabel);
        var start = skeleton.GetCurrentStartBoneId();
        var end = skeleton.GetCurrentEndBoneId();
        if (skeleton.IsValidBone(start) && skeleton.IsValidBone(end))
        {
            for (var i = (int)start; i < (int)end; ++i)
            {
                var boneName = OVRSkeleton.BoneLabelFromBoneId(skeleton.GetSkeletonType(), (OVRSkeleton.BoneId)i);
                EditorGUI.BeginChangeCheck();
                var val =
                    EditorGUILayout.ObjectField(boneName, skeleton.CustomBones[i], typeof(Transform), true);
                if (EditorGUI.EndChangeCheck())
                {
                    skeleton.CustomBones[i] = (Transform)val;
                    EditorUtility.SetDirty(skeleton);
                }
            }
        }
    }
}

/// <summary>
/// Extensions class for the editor methods of <see cref="OVRCustomSkeleton"/>.
/// </summary>
public static class OVRCustomSkeletonEditorExtensions
{
    /// <summary>
    /// This method tries to retarget the skeleton structure present in the current <see cref="GameObject"/> to the one supported by the body tracking system.
    /// </summary>
    /// <param name="customSkeleton" cref="OVRCustomSkeleton">The custom skeleton to run this method on</param>
    /// <param name="type" cref="OVRCustomSkeleton.RetargetingType">The skeleton structure to auto map from</param>
    public static void AutoMapBones(this OVRCustomSkeleton customSkeleton, OVRCustomSkeleton.RetargetingType type)
    {
        try
        {
            switch (type)
            {
                case OVRCustomSkeleton.RetargetingType.OculusSkeleton:
                    customSkeleton.AutoMapBonesFromOculusSkeleton();
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Invalid {nameof(OVRCustomSkeleton.RetargetingType)}");
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog($"Auto Map Bones Error", e.Message, "Ok");
        }
    }

    public static void TryAutoMapBonesByName(this OVRCustomSkeleton customSkeleton)
    {
        customSkeleton.AutoMapBonesFromOculusSkeleton();
    }

    internal static void AutoMapBonesFromOculusSkeleton(this OVRCustomSkeleton customSkeleton)
    {
        var start = customSkeleton.GetCurrentStartBoneId();
        var end = customSkeleton.GetCurrentEndBoneId();
        var skeletonType = customSkeleton.GetSkeletonType();
        if (customSkeleton.IsValidBone(start) && customSkeleton.IsValidBone(end))
        {
            for (var bi = (int)start; bi < (int)end; ++bi)
            {
                string fbxBoneName = FbxBoneNameFromBoneId(skeletonType, (OVRSkeleton.BoneId)bi);
                Transform t = customSkeleton.transform.FindChildRecursive(fbxBoneName);

                if (t == null && skeletonType == OVRSkeleton.SkeletonType.Body)
                {
                    var legacyBoneName = fbxBoneName
                        .Replace("Little", "Pinky")
                        .Replace("Metacarpal", "Meta");
                    t = customSkeleton.transform.FindChildRecursive(legacyBoneName);
                }

                if (t != null)
                {
                    customSkeleton.CustomBones[bi] = t;
                }
            }
        }
    }

    internal static bool ClearBonesMapping(this OVRCustomSkeleton skeleton)
    {
        var start = skeleton.GetCurrentStartBoneId();
        var end = skeleton.GetCurrentEndBoneId();
        var cleared = false;

        if (!skeleton.IsValidBone(start) || !skeleton.IsValidBone(end))
        {
            return false;
        }

        for (var i = (int)start; i < (int)end; ++i)
        {
            if (skeleton.CustomBones[i] != null)
            {
                skeleton.CustomBones[i] = null;
                cleared = true;
            }
        }

        return cleared;
    }

    private static string FbxBoneNameFromBoneId(OVRSkeleton.SkeletonType skeletonType, OVRSkeleton.BoneId bi)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.Body)
        {
            return FBXBodyBoneNames[(int)bi];
        }
        else
        {
            if (bi >= OVRSkeleton.BoneId.Hand_ThumbTip && bi <= OVRSkeleton.BoneId.Hand_PinkyTip)
            {
                return FBXHandSidePrefix[(int)skeletonType] +
                       FBXHandFingerNames[(int)bi - (int)OVRSkeleton.BoneId.Hand_ThumbTip] +
                       "_finger_tip_marker";
            }
            else
            {
                return FBXHandBonePrefix + FBXHandSidePrefix[(int)skeletonType] + FBXHandBoneNames[(int)bi];
            }
        }
    }

    private static readonly string[] FBXBodyBoneNames =
    {
        "Root",
        "Hips",
        "SpineLower",
        "SpineMiddle",
        "SpineUpper",
        "Chest",
        "Neck",
        "Head",
        "LeftShoulder",
        "LeftScapula",
        "LeftArmUpper",
        "LeftArmLower",
        "LeftHandWristTwist",
        "RightShoulder",
        "RightScapula",
        "RightArmUpper",
        "RightArmLower",
        "RightHandWristTwist",
        "LeftHandPalm",
        "LeftHandWrist",
        "LeftHandThumbMetacarpal",
        "LeftHandThumbProximal",
        "LeftHandThumbDistal",
        "LeftHandThumbTip",
        "LeftHandIndexMetacarpal",
        "LeftHandIndexProximal",
        "LeftHandIndexIntermediate",
        "LeftHandIndexDistal",
        "LeftHandIndexTip",
        "LeftHandMiddleMetacarpal",
        "LeftHandMiddleProximal",
        "LeftHandMiddleIntermediate",
        "LeftHandMiddleDistal",
        "LeftHandMiddleTip",
        "LeftHandRingMetacarpal",
        "LeftHandRingProximal",
        "LeftHandRingIntermediate",
        "LeftHandRingDistal",
        "LeftHandRingTip",
        "LeftHandLittleMetacarpal",
        "LeftHandLittleProximal",
        "LeftHandLittleIntermediate",
        "LeftHandLittleDistal",
        "LeftHandLittleTip",
        "RightHandPalm",
        "RightHandWrist",
        "RightHandThumbMetacarpal",
        "RightHandThumbProximal",
        "RightHandThumbDistal",
        "RightHandThumbTip",
        "RightHandIndexMetacarpal",
        "RightHandIndexProximal",
        "RightHandIndexIntermediate",
        "RightHandIndexDistal",
        "RightHandIndexTip",
        "RightHandMiddleMetacarpal",
        "RightHandMiddleProximal",
        "RightHandMiddleIntermediate",
        "RightHandMiddleDistal",
        "RightHandMiddleTip",
        "RightHandRingMetacarpal",
        "RightHandRingProximal",
        "RightHandRingIntermediate",
        "RightHandRingDistal",
        "RightHandRingTip",
        "RightHandLittleMetacarpal",
        "RightHandLittleProximal",
        "RightHandLittleIntermediate",
        "RightHandLittleDistal",
        "RightHandLittleTip"
    };


    private static readonly string[] FBXHandSidePrefix = { "l_", "r_" };
    private const string FBXHandBonePrefix = "b_";

    private static readonly string[] FBXHandBoneNames =
    {
        "wrist",
        "forearm_stub",
        "thumb0",
        "thumb1",
        "thumb2",
        "thumb3",
        "index1",
        "index2",
        "index3",
        "middle1",
        "middle2",
        "middle3",
        "ring1",
        "ring2",
        "ring3",
        "pinky0",
        "pinky1",
        "pinky2",
        "pinky3"
    };

    private static readonly string[] FBXHandFingerNames =
    {
        "thumb",
        "index",
        "middle",
        "ring",
        "pinky"
    };
}
