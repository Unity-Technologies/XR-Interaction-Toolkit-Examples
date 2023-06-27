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
using UnityEngine;

public partial class OVRUnityHumanoidSkeletonRetargeter
{
    /// <summary>
    /// This class contains mappings between Unity Humanoid Rig bones and Oculus Body Tracking bones
    /// </summary>
    protected static class OVRHumanBodyBonesMappings
    {
        /// <summary>
        /// Corresponds to major sections of the body (left food, chest, etc).
        /// </summary>
        public enum BodySection
        {
            LeftLeg,
            LeftFoot,
            RightLeg,
            RightFoot,
            LeftArm,
            LeftHand,
            RightArm,
            RightHand,
            Hips,
            Back,
            Neck,
            Head
        };

        /// <summary>
        /// Body tracking bone IDs that should be exposed through the inspector.
        /// BoneId has enum values that map to the same integers, which would not work
        /// with a serialized field that expects unique integers. BodyTrackingBoneId
        /// is an enum that restricts BoneId to the values that we care about.
        /// </summary>
        public enum BodyTrackingBoneId
        {
            Body_Start = OVRPlugin.BoneId.Body_Start,
            Body_Root = OVRPlugin.BoneId.Body_Root,
            Body_Hips = OVRPlugin.BoneId.Body_Hips,
            Body_SpineLower = OVRPlugin.BoneId.Body_SpineLower,
            Body_SpineMiddle = OVRPlugin.BoneId.Body_SpineMiddle,
            Body_SpineUpper = OVRPlugin.BoneId.Body_SpineUpper,
            Body_Chest = OVRPlugin.BoneId.Body_Chest,
            Body_Neck = OVRPlugin.BoneId.Body_Neck,
            Body_Head = OVRPlugin.BoneId.Body_Head,
            Body_LeftShoulder = OVRPlugin.BoneId.Body_LeftShoulder,
            Body_LeftScapula = OVRPlugin.BoneId.Body_LeftScapula,
            Body_LeftArmUpper = OVRPlugin.BoneId.Body_LeftArmUpper,
            Body_LeftArmLower = OVRPlugin.BoneId.Body_LeftArmLower,
            Body_LeftHandWristTwist = OVRPlugin.BoneId.Body_LeftHandWristTwist,
            Body_RightShoulder = OVRPlugin.BoneId.Body_RightShoulder,
            Body_RightScapula = OVRPlugin.BoneId.Body_RightScapula,
            Body_RightArmUpper = OVRPlugin.BoneId.Body_RightArmUpper,
            Body_RightArmLower = OVRPlugin.BoneId.Body_RightArmLower,
            Body_RightHandWristTwist = OVRPlugin.BoneId.Body_RightHandWristTwist,
            Body_LeftHandPalm = OVRPlugin.BoneId.Body_LeftHandPalm,
            Body_LeftHandWrist = OVRPlugin.BoneId.Body_LeftHandWrist,
            Body_LeftHandThumbMetacarpal = OVRPlugin.BoneId.Body_LeftHandThumbMetacarpal,
            Body_LeftHandThumbProximal = OVRPlugin.BoneId.Body_LeftHandThumbProximal,
            Body_LeftHandThumbDistal = OVRPlugin.BoneId.Body_LeftHandThumbDistal,
            Body_LeftHandThumbTip = OVRPlugin.BoneId.Body_LeftHandThumbTip,
            Body_LeftHandIndexMetacarpal = OVRPlugin.BoneId.Body_LeftHandIndexMetacarpal,
            Body_LeftHandIndexProximal = OVRPlugin.BoneId.Body_LeftHandIndexProximal,
            Body_LeftHandIndexIntermediate = OVRPlugin.BoneId.Body_LeftHandIndexIntermediate,
            Body_LeftHandIndexDistal = OVRPlugin.BoneId.Body_LeftHandIndexDistal,
            Body_LeftHandIndexTip = OVRPlugin.BoneId.Body_LeftHandIndexTip,
            Body_LeftHandMiddleMetacarpal = OVRPlugin.BoneId.Body_LeftHandMiddleMetacarpal,
            Body_LeftHandMiddleProximal = OVRPlugin.BoneId.Body_LeftHandMiddleProximal,
            Body_LeftHandMiddleIntermediate = OVRPlugin.BoneId.Body_LeftHandMiddleIntermediate,
            Body_LeftHandMiddleDistal = OVRPlugin.BoneId.Body_LeftHandMiddleDistal,
            Body_LeftHandMiddleTip = OVRPlugin.BoneId.Body_LeftHandMiddleTip,
            Body_LeftHandRingMetacarpal = OVRPlugin.BoneId.Body_LeftHandRingMetacarpal,
            Body_LeftHandRingProximal = OVRPlugin.BoneId.Body_LeftHandRingProximal,
            Body_LeftHandRingIntermediate = OVRPlugin.BoneId.Body_LeftHandRingIntermediate,
            Body_LeftHandRingDistal = OVRPlugin.BoneId.Body_LeftHandRingDistal,
            Body_LeftHandRingTip = OVRPlugin.BoneId.Body_LeftHandRingTip,
            Body_LeftHandLittleMetacarpal = OVRPlugin.BoneId.Body_LeftHandLittleMetacarpal,
            Body_LeftHandLittleProximal = OVRPlugin.BoneId.Body_LeftHandLittleProximal,
            Body_LeftHandLittleIntermediate = OVRPlugin.BoneId.Body_LeftHandLittleIntermediate,
            Body_LeftHandLittleDistal = OVRPlugin.BoneId.Body_LeftHandLittleDistal,
            Body_LeftHandLittleTip = OVRPlugin.BoneId.Body_LeftHandLittleTip,
            Body_RightHandPalm = OVRPlugin.BoneId.Body_RightHandPalm,
            Body_RightHandWrist = OVRPlugin.BoneId.Body_RightHandWrist,
            Body_RightHandThumbMetacarpal = OVRPlugin.BoneId.Body_RightHandThumbMetacarpal,
            Body_RightHandThumbProximal = OVRPlugin.BoneId.Body_RightHandThumbProximal,
            Body_RightHandThumbDistal = OVRPlugin.BoneId.Body_RightHandThumbDistal,
            Body_RightHandThumbTip = OVRPlugin.BoneId.Body_RightHandThumbTip,
            Body_RightHandIndexMetacarpal = OVRPlugin.BoneId.Body_RightHandIndexMetacarpal,
            Body_RightHandIndexProximal = OVRPlugin.BoneId.Body_RightHandIndexProximal,
            Body_RightHandIndexIntermediate = OVRPlugin.BoneId.Body_RightHandIndexIntermediate,
            Body_RightHandIndexDistal = OVRPlugin.BoneId.Body_RightHandIndexDistal,
            Body_RightHandIndexTip = OVRPlugin.BoneId.Body_RightHandIndexTip,
            Body_RightHandMiddleMetacarpal = OVRPlugin.BoneId.Body_RightHandMiddleMetacarpal,
            Body_RightHandMiddleProximal = OVRPlugin.BoneId.Body_RightHandMiddleProximal,
            Body_RightHandMiddleIntermediate = OVRPlugin.BoneId.Body_RightHandMiddleIntermediate,
            Body_RightHandMiddleDistal = OVRPlugin.BoneId.Body_RightHandMiddleDistal,
            Body_RightHandMiddleTip = OVRPlugin.BoneId.Body_RightHandMiddleTip,
            Body_RightHandRingMetacarpal = OVRPlugin.BoneId.Body_RightHandRingMetacarpal,
            Body_RightHandRingProximal = OVRPlugin.BoneId.Body_RightHandRingProximal,
            Body_RightHandRingIntermediate = OVRPlugin.BoneId.Body_RightHandRingIntermediate,
            Body_RightHandRingDistal = OVRPlugin.BoneId.Body_RightHandRingDistal,
            Body_RightHandRingTip = OVRPlugin.BoneId.Body_RightHandRingTip,
            Body_RightHandLittleMetacarpal = OVRPlugin.BoneId.Body_RightHandLittleMetacarpal,
            Body_RightHandLittleProximal = OVRPlugin.BoneId.Body_RightHandLittleProximal,
            Body_RightHandLittleIntermediate = OVRPlugin.BoneId.Body_RightHandLittleIntermediate,
            Body_RightHandLittleDistal = OVRPlugin.BoneId.Body_RightHandLittleDistal,
            Body_RightHandLittleTip = OVRPlugin.BoneId.Body_RightHandLittleTip,
            Body_End = OVRPlugin.BoneId.Body_End,

            // add new bones here

            NoOverride = OVRPlugin.BoneId.Body_End + 1,
            Remove = OVRPlugin.BoneId.Body_End + 2
        };

        /// <summary>
        /// For each humanoid bone, create a pair that determines the
        /// pair of bones that create the joint pair. Used to
        /// create the "axis" of the bone.
        /// </summary>
        public static readonly Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>>
            BoneToJointPair = new Dictionary<HumanBodyBones, Tuple<HumanBodyBones, HumanBodyBones>>()
            {
                {
                    HumanBodyBones.Neck,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Neck, HumanBodyBones.Head)
                },
                {
                    HumanBodyBones.Head,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Neck, HumanBodyBones.Head)
                },
                {
                    HumanBodyBones.LeftEye,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.LeftEye)
                },
                {
                    HumanBodyBones.RightEye,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.RightEye)
                },
                {
                    HumanBodyBones.Jaw,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Head, HumanBodyBones.Jaw)
                },

                {
                    HumanBodyBones.Hips,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Hips, HumanBodyBones.Spine)
                },
                {
                    HumanBodyBones.Spine,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Spine, HumanBodyBones.Chest)
                },
                {
                    HumanBodyBones.Chest,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.Chest, HumanBodyBones.UpperChest)
                },
                {
                    HumanBodyBones.UpperChest,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.UpperChest, HumanBodyBones.Neck)
                },

                {
                    HumanBodyBones.LeftShoulder,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm)
                },
                {
                    HumanBodyBones.LeftUpperArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm)
                },
                {
                    HumanBodyBones.LeftLowerArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand)
                },
                {
                    HumanBodyBones.LeftHand,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftHand,
                        HumanBodyBones.LeftMiddleProximal)
                },

                {
                    HumanBodyBones.RightShoulder,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightShoulder,
                        HumanBodyBones.RightUpperArm)
                },
                {
                    HumanBodyBones.RightUpperArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightUpperArm,
                        HumanBodyBones.RightLowerArm)
                },
                {
                    HumanBodyBones.RightLowerArm,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand)
                },
                {
                    HumanBodyBones.RightHand,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightHand,
                        HumanBodyBones.RightMiddleProximal)
                },

                {
                    HumanBodyBones.RightUpperLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightUpperLeg,
                        HumanBodyBones.RightLowerLeg)
                },
                {
                    HumanBodyBones.RightLowerLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot)
                },
                {
                    HumanBodyBones.RightFoot,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)
                },
                {
                    HumanBodyBones.RightToes,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)
                },

                {
                    HumanBodyBones.LeftUpperLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg)
                },
                {
                    HumanBodyBones.LeftLowerLeg,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot)
                },
                {
                    HumanBodyBones.LeftFoot,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes)
                },
                {
                    HumanBodyBones.LeftToes,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes)
                },

                {
                    HumanBodyBones.LeftThumbProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbProximal,
                        HumanBodyBones.LeftThumbIntermediate)
                },
                {
                    HumanBodyBones.LeftThumbIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbIntermediate,
                        HumanBodyBones.LeftThumbDistal)
                },
                {
                    HumanBodyBones.LeftThumbDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftThumbDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftIndexProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexProximal,
                        HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    HumanBodyBones.LeftIndexIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexIntermediate,
                        HumanBodyBones.LeftIndexDistal)
                },
                {
                    HumanBodyBones.LeftIndexDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftIndexDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftMiddleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleProximal,
                        HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    HumanBodyBones.LeftMiddleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleIntermediate,
                        HumanBodyBones.LeftMiddleDistal)
                },
                {
                    HumanBodyBones.LeftMiddleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftRingProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingProximal,
                        HumanBodyBones.LeftRingIntermediate)
                },
                {
                    HumanBodyBones.LeftRingIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingIntermediate,
                        HumanBodyBones.LeftRingDistal)
                },
                {
                    HumanBodyBones.LeftRingDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftRingDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.LeftLittleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleProximal,
                        HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    HumanBodyBones.LeftLittleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleIntermediate,
                        HumanBodyBones.LeftLittleDistal)
                },
                {
                    HumanBodyBones.LeftLittleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.LeftLittleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightThumbProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbProximal,
                        HumanBodyBones.RightThumbIntermediate)
                },
                {
                    HumanBodyBones.RightThumbIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbIntermediate,
                        HumanBodyBones.RightThumbDistal)
                },
                {
                    HumanBodyBones.RightThumbDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightThumbDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightIndexProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexProximal,
                        HumanBodyBones.RightIndexIntermediate)
                },
                {
                    HumanBodyBones.RightIndexIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexIntermediate,
                        HumanBodyBones.RightIndexDistal)
                },
                {
                    HumanBodyBones.RightIndexDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightIndexDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightMiddleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleProximal,
                        HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    HumanBodyBones.RightMiddleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleIntermediate,
                        HumanBodyBones.RightMiddleDistal)
                },
                {
                    HumanBodyBones.RightMiddleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightMiddleDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightRingProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingProximal,
                        HumanBodyBones.RightRingIntermediate)
                },
                {
                    HumanBodyBones.RightRingIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingIntermediate,
                        HumanBodyBones.RightRingDistal)
                },
                {
                    HumanBodyBones.RightRingDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightRingDistal, HumanBodyBones.LastBone)
                }, // Invalid.

                {
                    HumanBodyBones.RightLittleProximal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleProximal,
                        HumanBodyBones.RightLittleIntermediate)
                },
                {
                    HumanBodyBones.RightLittleIntermediate,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleIntermediate,
                        HumanBodyBones.RightLittleDistal)
                },
                {
                    HumanBodyBones.RightLittleDistal,
                    new Tuple<HumanBodyBones, HumanBodyBones>(HumanBodyBones.RightLittleDistal, HumanBodyBones.LastBone)
                }, // Invalid.
            };

        /// <summary>
        /// Paired human body bones with body sections.
        /// </summary>
        public static readonly Dictionary<HumanBodyBones, BodySection> BoneToBodySection =
            new Dictionary<HumanBodyBones, BodySection>()
            {
                { HumanBodyBones.Neck, BodySection.Neck },

                { HumanBodyBones.Head, BodySection.Head },
                { HumanBodyBones.LeftEye, BodySection.Head },
                { HumanBodyBones.RightEye, BodySection.Head },
                { HumanBodyBones.Jaw, BodySection.Head },

                { HumanBodyBones.Hips, BodySection.Hips },

                { HumanBodyBones.Spine, BodySection.Back },
                { HumanBodyBones.Chest, BodySection.Back },
                { HumanBodyBones.UpperChest, BodySection.Back },

                { HumanBodyBones.RightShoulder, BodySection.RightArm },
                { HumanBodyBones.RightUpperArm, BodySection.RightArm },
                { HumanBodyBones.RightLowerArm, BodySection.RightArm },
                { HumanBodyBones.RightHand, BodySection.RightArm },

                { HumanBodyBones.LeftShoulder, BodySection.LeftArm },
                { HumanBodyBones.LeftUpperArm, BodySection.LeftArm },
                { HumanBodyBones.LeftLowerArm, BodySection.LeftArm },
                { HumanBodyBones.LeftHand, BodySection.LeftArm },

                { HumanBodyBones.LeftUpperLeg, BodySection.LeftLeg },
                { HumanBodyBones.LeftLowerLeg, BodySection.LeftLeg },

                { HumanBodyBones.LeftFoot, BodySection.LeftFoot },
                { HumanBodyBones.LeftToes, BodySection.LeftFoot },

                { HumanBodyBones.RightUpperLeg, BodySection.RightLeg },
                { HumanBodyBones.RightLowerLeg, BodySection.RightLeg },

                { HumanBodyBones.RightFoot, BodySection.RightFoot },
                { HumanBodyBones.RightToes, BodySection.RightFoot },

                { HumanBodyBones.LeftThumbProximal, BodySection.LeftHand },
                { HumanBodyBones.LeftThumbIntermediate, BodySection.LeftHand },
                { HumanBodyBones.LeftThumbDistal, BodySection.LeftHand },
                { HumanBodyBones.LeftIndexProximal, BodySection.LeftHand },
                { HumanBodyBones.LeftIndexIntermediate, BodySection.LeftHand },
                { HumanBodyBones.LeftIndexDistal, BodySection.LeftHand },
                { HumanBodyBones.LeftMiddleProximal, BodySection.LeftHand },
                { HumanBodyBones.LeftMiddleIntermediate, BodySection.LeftHand },
                { HumanBodyBones.LeftMiddleDistal, BodySection.LeftHand },
                { HumanBodyBones.LeftRingProximal, BodySection.LeftHand },
                { HumanBodyBones.LeftRingIntermediate, BodySection.LeftHand },
                { HumanBodyBones.LeftRingDistal, BodySection.LeftHand },
                { HumanBodyBones.LeftLittleProximal, BodySection.LeftHand },
                { HumanBodyBones.LeftLittleIntermediate, BodySection.LeftHand },
                { HumanBodyBones.LeftLittleDistal, BodySection.LeftHand },

                { HumanBodyBones.RightThumbProximal, BodySection.RightHand },
                { HumanBodyBones.RightThumbIntermediate, BodySection.RightHand },
                { HumanBodyBones.RightThumbDistal, BodySection.RightHand },
                { HumanBodyBones.RightIndexProximal, BodySection.RightHand },
                { HumanBodyBones.RightIndexIntermediate, BodySection.RightHand },
                { HumanBodyBones.RightIndexDistal, BodySection.RightHand },
                { HumanBodyBones.RightMiddleProximal, BodySection.RightHand },
                { HumanBodyBones.RightMiddleIntermediate, BodySection.RightHand },
                { HumanBodyBones.RightMiddleDistal, BodySection.RightHand },
                { HumanBodyBones.RightRingProximal, BodySection.RightHand },
                { HumanBodyBones.RightRingIntermediate, BodySection.RightHand },
                { HumanBodyBones.RightRingDistal, BodySection.RightHand },
                { HumanBodyBones.RightLittleProximal, BodySection.RightHand },
                { HumanBodyBones.RightLittleIntermediate, BodySection.RightHand },
                { HumanBodyBones.RightLittleDistal, BodySection.RightHand },
            };

        /// <summary>
        /// Paired OVRSkeleton bones with human body bones.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, HumanBodyBones> BoneIdToHumanBodyBone =
            new Dictionary<OVRSkeleton.BoneId, HumanBodyBones>()
            {
                { OVRSkeleton.BoneId.Body_Hips, HumanBodyBones.Hips },
                { OVRSkeleton.BoneId.Body_SpineLower, HumanBodyBones.Spine },
                { OVRSkeleton.BoneId.Body_SpineUpper, HumanBodyBones.Chest },
                { OVRSkeleton.BoneId.Body_Chest, HumanBodyBones.UpperChest },
                { OVRSkeleton.BoneId.Body_Neck, HumanBodyBones.Neck },
                { OVRSkeleton.BoneId.Body_Head, HumanBodyBones.Head },

                { OVRSkeleton.BoneId.Body_LeftShoulder, HumanBodyBones.LeftShoulder },
                { OVRSkeleton.BoneId.Body_LeftArmUpper, HumanBodyBones.LeftUpperArm },
                { OVRSkeleton.BoneId.Body_LeftArmLower, HumanBodyBones.LeftLowerArm },
                { OVRSkeleton.BoneId.Body_LeftHandWrist, HumanBodyBones.LeftHand },

                { OVRSkeleton.BoneId.Body_RightShoulder, HumanBodyBones.RightShoulder },
                { OVRSkeleton.BoneId.Body_RightArmUpper, HumanBodyBones.RightUpperArm },
                { OVRSkeleton.BoneId.Body_RightArmLower, HumanBodyBones.RightLowerArm },
                { OVRSkeleton.BoneId.Body_RightHandWrist, HumanBodyBones.RightHand },

                { OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal, HumanBodyBones.LeftThumbProximal },
                { OVRSkeleton.BoneId.Body_LeftHandThumbProximal, HumanBodyBones.LeftThumbIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandThumbDistal, HumanBodyBones.LeftThumbDistal },
                { OVRSkeleton.BoneId.Body_LeftHandIndexProximal, HumanBodyBones.LeftIndexProximal },
                { OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate, HumanBodyBones.LeftIndexIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandIndexDistal, HumanBodyBones.LeftIndexDistal },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleProximal, HumanBodyBones.LeftMiddleProximal },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate, HumanBodyBones.LeftMiddleIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandMiddleDistal, HumanBodyBones.LeftMiddleDistal },
                { OVRSkeleton.BoneId.Body_LeftHandRingProximal, HumanBodyBones.LeftRingProximal },
                { OVRSkeleton.BoneId.Body_LeftHandRingIntermediate, HumanBodyBones.LeftRingIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandRingDistal, HumanBodyBones.LeftRingDistal },
                { OVRSkeleton.BoneId.Body_LeftHandLittleProximal, HumanBodyBones.LeftLittleProximal },
                { OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate, HumanBodyBones.LeftLittleIntermediate },
                { OVRSkeleton.BoneId.Body_LeftHandLittleDistal, HumanBodyBones.LeftLittleDistal },

                { OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal, HumanBodyBones.RightThumbProximal },
                { OVRSkeleton.BoneId.Body_RightHandThumbProximal, HumanBodyBones.RightThumbIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandThumbDistal, HumanBodyBones.RightThumbDistal },
                { OVRSkeleton.BoneId.Body_RightHandIndexProximal, HumanBodyBones.RightIndexProximal },
                { OVRSkeleton.BoneId.Body_RightHandIndexIntermediate, HumanBodyBones.RightIndexIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandIndexDistal, HumanBodyBones.RightIndexDistal },
                { OVRSkeleton.BoneId.Body_RightHandMiddleProximal, HumanBodyBones.RightMiddleProximal },
                { OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate, HumanBodyBones.RightMiddleIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandMiddleDistal, HumanBodyBones.RightMiddleDistal },
                { OVRSkeleton.BoneId.Body_RightHandRingProximal, HumanBodyBones.RightRingProximal },
                { OVRSkeleton.BoneId.Body_RightHandRingIntermediate, HumanBodyBones.RightRingIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandRingDistal, HumanBodyBones.RightRingDistal },
                { OVRSkeleton.BoneId.Body_RightHandLittleProximal, HumanBodyBones.RightLittleProximal },
                { OVRSkeleton.BoneId.Body_RightHandLittleIntermediate, HumanBodyBones.RightLittleIntermediate },
                { OVRSkeleton.BoneId.Body_RightHandLittleDistal, HumanBodyBones.RightLittleDistal },
            };

        /// <summary>
        /// For each humanoid bone, create a pair that determines the
        /// pair of bones that create the joint pair. Used to
        /// create the "axis" of the bone.
        /// </summary>
        public static readonly Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>>
            BoneIdToJointPair = new Dictionary<OVRSkeleton.BoneId, Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>>()
            {
                {
                    OVRSkeleton.BoneId.Body_Neck,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_Neck,
                        OVRSkeleton.BoneId.Body_Head)
                },
                {
                    OVRSkeleton.BoneId.Body_Head,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_Head,
                        OVRSkeleton.BoneId.Invalid)
                },

                {
                    OVRSkeleton.BoneId.Body_Root,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_Root,
                        OVRSkeleton.BoneId.Body_Hips)
                },
                {
                    OVRSkeleton.BoneId.Body_Hips,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_Hips,
                        OVRSkeleton.BoneId.Body_SpineLower)
                },
                {
                    OVRSkeleton.BoneId.Body_SpineLower,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_SpineLower,
                        OVRSkeleton.BoneId.Body_SpineMiddle)
                },
                {
                    OVRSkeleton.BoneId.Body_SpineMiddle,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_SpineMiddle,
                        OVRSkeleton.BoneId.Body_SpineUpper)
                },
                {
                    OVRSkeleton.BoneId.Body_SpineUpper,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_SpineUpper,
                        OVRSkeleton.BoneId.Body_Chest)
                },
                {
                    OVRSkeleton.BoneId.Body_Chest,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_Chest,
                        OVRSkeleton.BoneId.Body_Neck)
                },

                {
                    OVRSkeleton.BoneId.Body_LeftShoulder,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftShoulder,
                        OVRSkeleton.BoneId.Body_LeftArmUpper)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftScapula,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftScapula,
                        OVRSkeleton.BoneId.Body_LeftArmUpper)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftArmUpper,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftArmUpper,
                        OVRSkeleton.BoneId.Body_LeftArmLower)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftArmLower,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftArmLower,
                        OVRSkeleton.BoneId.Body_LeftHandWrist)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandWrist,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandWrist,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandPalm,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandPalm,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandWristTwist,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandWristTwist,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal)
                },

                {
                    OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandThumbMetacarpal,
                        OVRSkeleton.BoneId.Body_LeftHandThumbProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandThumbProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandThumbProximal,
                        OVRSkeleton.BoneId.Body_LeftHandThumbDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandThumbDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandThumbDistal,
                        OVRSkeleton.BoneId.Body_LeftHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandThumbTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandThumbDistal,
                        OVRSkeleton.BoneId.Body_LeftHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandIndexMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandIndexMetacarpal,
                        OVRSkeleton.BoneId.Body_LeftHandIndexProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandIndexProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandIndexProximal,
                        OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandIndexIntermediate,
                        OVRSkeleton.BoneId.Body_LeftHandIndexDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandIndexDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandIndexDistal,
                        OVRSkeleton.BoneId.Body_LeftHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandIndexTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandIndexDistal,
                        OVRSkeleton.BoneId.Body_LeftHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandMiddleMetacarpal,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandMiddleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandMiddleProximal,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.Body_LeftHandMiddleIntermediate,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandMiddleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandMiddleDistal,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandMiddleTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandMiddleDistal,
                        OVRSkeleton.BoneId.Body_LeftHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandRingMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandRingMetacarpal,
                        OVRSkeleton.BoneId.Body_LeftHandRingProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandRingProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandRingProximal,
                        OVRSkeleton.BoneId.Body_LeftHandRingIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandRingIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandRingIntermediate,
                        OVRSkeleton.BoneId.Body_LeftHandRingDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandRingDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandRingDistal,
                        OVRSkeleton.BoneId.Body_LeftHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandRingTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandRingDistal,
                        OVRSkeleton.BoneId.Body_LeftHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandLittleMetacarpal,
                        OVRSkeleton.BoneId.Body_LeftHandLittleProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandLittleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandLittleProximal,
                        OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.Body_LeftHandLittleIntermediate,
                        OVRSkeleton.BoneId.Body_LeftHandLittleDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandLittleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandLittleDistal,
                        OVRSkeleton.BoneId.Body_LeftHandLittleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_LeftHandLittleTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_LeftHandLittleDistal,
                        OVRSkeleton.BoneId.Body_LeftHandRingTip)
                },

                {
                    OVRSkeleton.BoneId.Body_RightShoulder,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightShoulder,
                        OVRSkeleton.BoneId.Body_RightArmUpper)
                },
                {
                    OVRSkeleton.BoneId.Body_RightScapula,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightScapula,
                        OVRSkeleton.BoneId.Body_RightArmUpper)
                },
                {
                    OVRSkeleton.BoneId.Body_RightArmUpper,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightArmUpper,
                        OVRSkeleton.BoneId.Body_RightArmLower)
                },
                {
                    OVRSkeleton.BoneId.Body_RightArmLower,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightArmLower,
                        OVRSkeleton.BoneId.Body_RightHandWrist)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandWrist,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandWrist,
                        OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandPalm,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandPalm,
                        OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandWristTwist,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandWristTwist,
                        OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal)
                },

                {
                    OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandThumbMetacarpal,
                        OVRSkeleton.BoneId.Body_RightHandThumbProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandThumbProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandThumbProximal,
                        OVRSkeleton.BoneId.Body_RightHandThumbDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandThumbDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandThumbDistal,
                        OVRSkeleton.BoneId.Body_RightHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandThumbTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandThumbDistal,
                        OVRSkeleton.BoneId.Body_RightHandThumbTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandIndexMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandIndexMetacarpal,
                        OVRSkeleton.BoneId.Body_RightHandIndexProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandIndexProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandIndexProximal,
                        OVRSkeleton.BoneId.Body_RightHandIndexIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandIndexIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.Body_RightHandIndexIntermediate,
                        OVRSkeleton.BoneId.Body_RightHandIndexDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandIndexDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandIndexDistal,
                        OVRSkeleton.BoneId.Body_RightHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandIndexTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandIndexDistal,
                        OVRSkeleton.BoneId.Body_RightHandIndexTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandMiddleMetacarpal,
                        OVRSkeleton.BoneId.Body_RightHandMiddleProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandMiddleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandMiddleProximal,
                        OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.Body_RightHandMiddleIntermediate,
                        OVRSkeleton.BoneId.Body_RightHandMiddleDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandMiddleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandMiddleDistal,
                        OVRSkeleton.BoneId.Body_RightHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandMiddleTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandMiddleDistal,
                        OVRSkeleton.BoneId.Body_RightHandMiddleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandRingMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandRingMetacarpal,
                        OVRSkeleton.BoneId.Body_RightHandRingProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandRingProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandRingProximal,
                        OVRSkeleton.BoneId.Body_RightHandRingIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandRingIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandRingIntermediate,
                        OVRSkeleton.BoneId.Body_RightHandRingDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandRingDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandRingDistal,
                        OVRSkeleton.BoneId.Body_RightHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandRingTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandRingDistal,
                        OVRSkeleton.BoneId.Body_RightHandRingTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandLittleMetacarpal,
                        OVRSkeleton.BoneId.Body_RightHandLittleProximal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandLittleProximal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandLittleProximal,
                        OVRSkeleton.BoneId.Body_RightHandLittleIntermediate)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandLittleIntermediate,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(
                        OVRSkeleton.BoneId.Body_RightHandLittleIntermediate,
                        OVRSkeleton.BoneId.Body_RightHandLittleDistal)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandLittleDistal,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandLittleDistal,
                        OVRSkeleton.BoneId.Body_RightHandLittleTip)
                },
                {
                    OVRSkeleton.BoneId.Body_RightHandLittleTip,
                    new Tuple<OVRSkeleton.BoneId, OVRSkeleton.BoneId>(OVRSkeleton.BoneId.Body_RightHandLittleDistal,
                        OVRSkeleton.BoneId.Body_RightHandRingTip)
                },
            };
    }
}
