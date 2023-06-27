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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Primitive type serialization
/// </summary>
namespace Oculus.Interaction.Body.Input
{
    public static class Constants
    {
        public const int NUM_BODY_JOINTS = (int)BodyJointId.Body_End;
    }

    public enum BodyJointId
    {
        Invalid = -1,

        [InspectorName("Body Start")]
        Body_Start = 0,
        [InspectorName("Root")]
        Body_Root = Body_Start + 0,
        [InspectorName("Hips")]
        Body_Hips = Body_Start + 1,
        [InspectorName("Spine Lower")]
        Body_SpineLower = Body_Start + 2,
        [InspectorName("Spine Middle")]
        Body_SpineMiddle = Body_Start + 3,
        [InspectorName("Spine Upper")]
        Body_SpineUpper = Body_Start + 4,
        [InspectorName("Chest")]
        Body_Chest = Body_Start + 5,
        [InspectorName("Neck")]
        Body_Neck = Body_Start + 6,
        [InspectorName("Head")]
        Body_Head = Body_Start + 7,
        [InspectorName("Left Arm/Left Shoulder")]
        Body_LeftShoulder = Body_Start + 8,
        [InspectorName("Left Arm/Left Scapula")]
        Body_LeftScapula = Body_Start + 9,
        [InspectorName("Left Arm/Left Arm Upper")]
        Body_LeftArmUpper = Body_Start + 10,
        [InspectorName("Left Arm/Left Arm Lower")]
        Body_LeftArmLower = Body_Start + 11,
        [InspectorName("Left Arm/Left Hand Wrist Twist")]
        Body_LeftHandWristTwist = Body_Start + 12,
        [InspectorName("Right Arm/Right Shoulder")]
        Body_RightShoulder = Body_Start + 13,
        [InspectorName("Right Arm/Right Scapula")]
        Body_RightScapula = Body_Start + 14,
        [InspectorName("Right Arm/Right Arm Upper")]
        Body_RightArmUpper = Body_Start + 15,
        [InspectorName("Right Arm/Right Arm Lower")]
        Body_RightArmLower = Body_Start + 16,
        [InspectorName("Right Arm/Right Hand Wrist Twist")]
        Body_RightHandWristTwist = Body_Start + 17,

        [InspectorName("Left Hand/Left Hand Wrist")]
        Body_LeftHandWrist = Body_Start + 18,
        [InspectorName("Left Hand/Left Hand Palm")]
        Body_LeftHandPalm = Body_Start + 19,
        [InspectorName("Left Hand/Left Hand Thumb Metacarpal")]
        Body_LeftHandThumbMetacarpal = Body_Start + 20,
        [InspectorName("Left Hand/Left Hand Thumb Proximal")]
        Body_LeftHandThumbProximal = Body_Start + 21,
        [InspectorName("Left Hand/Left Hand Thumb Distal")]
        Body_LeftHandThumbDistal = Body_Start + 22,
        [InspectorName("Left Hand/Left Hand Thumb Tip")]
        Body_LeftHandThumbTip = Body_Start + 23,
        [InspectorName("Left Hand/Left Hand Index Metacarpal")]
        Body_LeftHandIndexMetacarpal = Body_Start + 24,
        [InspectorName("Left Hand/Left Hand Index Proximal")]
        Body_LeftHandIndexProximal = Body_Start + 25,
        [InspectorName("Left Hand/Left Hand Index Intermediate")]
        Body_LeftHandIndexIntermediate = Body_Start + 26,
        [InspectorName("Left Hand/Left Hand Index Distal")]
        Body_LeftHandIndexDistal = Body_Start + 27,
        [InspectorName("Left Hand/Left Hand Index Tip")]
        Body_LeftHandIndexTip = Body_Start + 28,
        [InspectorName("Left Hand/Left Hand Middle Metacarpal")]
        Body_LeftHandMiddleMetacarpal = Body_Start + 29,
        [InspectorName("Left Hand/Left Hand Middle Proximal")]
        Body_LeftHandMiddleProximal = Body_Start + 30,
        [InspectorName("Left Hand/Left Hand Middle Intermediate")]
        Body_LeftHandMiddleIntermediate = Body_Start + 31,
        [InspectorName("Left Hand/Left Hand Middle Distal")]
        Body_LeftHandMiddleDistal = Body_Start + 32,
        [InspectorName("Left Hand/Left Hand Middle Tip")]
        Body_LeftHandMiddleTip = Body_Start + 33,
        [InspectorName("Left Hand/Left Hand Ring Metacarpal")]
        Body_LeftHandRingMetacarpal = Body_Start + 34,
        [InspectorName("Left Hand/Left Hand Ring Proximal")]
        Body_LeftHandRingProximal = Body_Start + 35,
        [InspectorName("Left Hand/Left Hand Ring Intermediate")]
        Body_LeftHandRingIntermediate = Body_Start + 36,
        [InspectorName("Left Hand/Left Hand Ring Distal")]
        Body_LeftHandRingDistal = Body_Start + 37,
        [InspectorName("Left Hand/Left Hand Ring Tip")]
        Body_LeftHandRingTip = Body_Start + 38,
        [InspectorName("Left Hand/Left Hand Little Metacarpal")]
        Body_LeftHandLittleMetacarpal = Body_Start + 39,
        [InspectorName("Left Hand/Left Hand Little Proximal")]
        Body_LeftHandLittleProximal = Body_Start + 40,
        [InspectorName("Left Hand/Left Hand Little Intermediate")]
        Body_LeftHandLittleIntermediate = Body_Start + 41,
        [InspectorName("Left Hand/Left Hand Little Distal")]
        Body_LeftHandLittleDistal = Body_Start + 42,
        [InspectorName("Left Hand/Left Hand Little Tip")]
        Body_LeftHandLittleTip = Body_Start + 43,

        [InspectorName("Right Hand/Right Hand Wrist")]
        Body_RightHandWrist = Body_Start + 44,
        [InspectorName("Right Hand/Right Hand Palm")]
        Body_RightHandPalm = Body_Start + 45,
        [InspectorName("Right Hand/Right Hand Thumb Metacarpal")]
        Body_RightHandThumbMetacarpal = Body_Start + 46,
        [InspectorName("Right Hand/Right Hand Thumb Proximal")]
        Body_RightHandThumbProximal = Body_Start + 47,
        [InspectorName("Right Hand/Right Hand Thumb Distal")]
        Body_RightHandThumbDistal = Body_Start + 48,
        [InspectorName("Right Hand/Right Hand Thumb Tip")]
        Body_RightHandThumbTip = Body_Start + 49,
        [InspectorName("Right Hand/Right Hand Index Metacarpal")]
        Body_RightHandIndexMetacarpal = Body_Start + 50,
        [InspectorName("Right Hand/Right Hand Index Proximal")]
        Body_RightHandIndexProximal = Body_Start + 51,
        [InspectorName("Right Hand/Right Hand Index Intermediate")]
        Body_RightHandIndexIntermediate = Body_Start + 52,
        [InspectorName("Right Hand/Right Hand Index Distal")]
        Body_RightHandIndexDistal = Body_Start + 53,
        [InspectorName("Right Hand/Right Hand Index Tip")]
        Body_RightHandIndexTip = Body_Start + 54,
        [InspectorName("Right Hand/Right Hand Middle Metacarpal")]
        Body_RightHandMiddleMetacarpal = Body_Start + 55,
        [InspectorName("Right Hand/Right Hand Middle Proximal")]
        Body_RightHandMiddleProximal = Body_Start + 56,
        [InspectorName("Right Hand/Right Hand Middle Intermediate")]
        Body_RightHandMiddleIntermediate = Body_Start + 57,
        [InspectorName("Right Hand/Right Hand Middle Distal")]
        Body_RightHandMiddleDistal = Body_Start + 58,
        [InspectorName("Right Hand/Right Hand Middle Tip")]
        Body_RightHandMiddleTip = Body_Start + 59,
        [InspectorName("Right Hand/Right Hand Ring Metacarpal")]
        Body_RightHandRingMetacarpal = Body_Start + 60,
        [InspectorName("Right Hand/Right Hand Ring Proximal")]
        Body_RightHandRingProximal = Body_Start + 61,
        [InspectorName("Right Hand/Right Hand Ring Intermediate")]
        Body_RightHandRingIntermediate = Body_Start + 62,
        [InspectorName("Right Hand/Right Hand Ring Distal")]
        Body_RightHandRingDistal = Body_Start + 63,
        [InspectorName("Right Hand/Right Hand Ring Tip")]
        Body_RightHandRingTip = Body_Start + 64,
        [InspectorName("Right Hand/Right Hand Little Metacarpal")]
        Body_RightHandLittleMetacarpal = Body_Start + 65,
        [InspectorName("Right Hand/Right Hand Little Proximal")]
        Body_RightHandLittleProximal = Body_Start + 66,
        [InspectorName("Right Hand/Right Hand Little Intermediate")]
        Body_RightHandLittleIntermediate = Body_Start + 67,
        [InspectorName("Right Hand/Right Hand Little Distal")]
        Body_RightHandLittleDistal = Body_Start + 68,
        [InspectorName("Right Hand/Right Hand Little Tip")]
        Body_RightHandLittleTip = Body_Start + 69,
        [InspectorName("Body End")]
        Body_End = Body_Start + 70,
    }

    public class ReadOnlyBodyJointPoses : IReadOnlyList<Pose>
    {
        private Pose[] _poses;

        public ReadOnlyBodyJointPoses(Pose[] poses)
        {
            _poses = poses;
        }

        public IEnumerator<Pose> GetEnumerator()
        {
            foreach (var pose in _poses)
            {
                yield return pose;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static ReadOnlyBodyJointPoses Empty { get; } = new ReadOnlyBodyJointPoses(Array.Empty<Pose>());

        public int Count => _poses.Length;

        public Pose this[int index] => _poses[index];

        public ref readonly Pose this[BodyJointId index] => ref _poses[(int)index];
    }
}
