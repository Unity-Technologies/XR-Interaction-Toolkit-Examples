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

using System.Collections.Generic;

/// <summary>
/// Primitive type serialization
/// </summary>
namespace Oculus.Interaction.Body.Input
{
    using OVRBoneId = OVRPlugin.BoneId;

    public class OVRSkeletonMapping : BodySkeletonMapping<OVRBoneId>, ISkeletonMapping
    {
        protected override IReadOnlyDictionary<BodyJointId, JointInfo> GetJointMapping()
        {
            return _jointMapping;
        }

        protected override OVRBoneId GetRoot()
        {
            return OVRBoneId.Body_Root;
        }

        /// <summary>
        /// Mapping of <see cref="BodyJointId"/> to <see cref="OVRBoneId"/> and parent joint
        /// </summary>
        private readonly Dictionary<BodyJointId, JointInfo> _jointMapping =
            new Dictionary<BodyJointId, JointInfo>
        {
            [BodyJointId.Body_Root] =                           new JointInfo(OVRBoneId.Body_Root, OVRBoneId.Body_Root),
            [BodyJointId.Body_Hips] =                           new JointInfo(OVRBoneId.Body_Hips, OVRBoneId.Body_Root),
            [BodyJointId.Body_SpineLower] =                     new JointInfo(OVRBoneId.Body_SpineLower, OVRBoneId.Body_Hips),
            [BodyJointId.Body_SpineMiddle] =                    new JointInfo(OVRBoneId.Body_SpineMiddle, OVRBoneId.Body_SpineLower),
            [BodyJointId.Body_SpineUpper] =                     new JointInfo(OVRBoneId.Body_SpineUpper, OVRBoneId.Body_SpineMiddle),
            [BodyJointId.Body_Chest] =                          new JointInfo(OVRBoneId.Body_Chest, OVRBoneId.Body_SpineUpper),
            [BodyJointId.Body_Neck] =                           new JointInfo(OVRBoneId.Body_Neck, OVRBoneId.Body_Chest),
            [BodyJointId.Body_Head] =                           new JointInfo(OVRBoneId.Body_Head, OVRBoneId.Body_Neck),

            // Left Arm
            [BodyJointId.Body_LeftShoulder] =                   new JointInfo(OVRBoneId.Body_LeftShoulder, OVRBoneId.Body_Chest),
            [BodyJointId.Body_LeftScapula] =                    new JointInfo(OVRBoneId.Body_LeftScapula, OVRBoneId.Body_LeftShoulder),
            [BodyJointId.Body_LeftArmUpper] =                   new JointInfo(OVRBoneId.Body_LeftArmUpper, OVRBoneId.Body_LeftScapula),
            [BodyJointId.Body_LeftArmLower] =                   new JointInfo(OVRBoneId.Body_LeftArmLower, OVRBoneId.Body_LeftArmUpper),
            [BodyJointId.Body_LeftHandWristTwist] =             new JointInfo(OVRBoneId.Body_LeftHandWristTwist, OVRBoneId.Body_LeftArmLower),

            // Right Arm
            [BodyJointId.Body_RightShoulder] =                  new JointInfo(OVRBoneId.Body_RightShoulder, OVRBoneId.Body_Chest),
            [BodyJointId.Body_RightScapula] =                   new JointInfo(OVRBoneId.Body_RightScapula, OVRBoneId.Body_RightShoulder),
            [BodyJointId.Body_RightArmUpper] =                  new JointInfo(OVRBoneId.Body_RightArmUpper, OVRBoneId.Body_RightScapula),
            [BodyJointId.Body_RightArmLower] =                  new JointInfo(OVRBoneId.Body_RightArmLower, OVRBoneId.Body_RightArmUpper),
            [BodyJointId.Body_RightHandWristTwist] =            new JointInfo(OVRBoneId.Body_RightHandWristTwist, OVRBoneId.Body_RightArmLower),

            // Left Hand
            [BodyJointId.Body_LeftHandWrist] =                  new JointInfo(OVRBoneId.Body_LeftHandWrist, OVRBoneId.Body_LeftArmLower),
            [BodyJointId.Body_LeftHandPalm] =                   new JointInfo(OVRBoneId.Body_LeftHandPalm, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandThumbMetacarpal] =        new JointInfo(OVRBoneId.Body_LeftHandThumbMetacarpal, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandThumbProximal] =          new JointInfo(OVRBoneId.Body_LeftHandThumbProximal, OVRBoneId.Body_LeftHandThumbMetacarpal),
            [BodyJointId.Body_LeftHandThumbDistal] =            new JointInfo(OVRBoneId.Body_LeftHandThumbDistal, OVRBoneId.Body_LeftHandThumbProximal),
            [BodyJointId.Body_LeftHandThumbTip] =               new JointInfo(OVRBoneId.Body_LeftHandThumbTip, OVRBoneId.Body_LeftHandThumbDistal),
            [BodyJointId.Body_LeftHandIndexMetacarpal] =        new JointInfo(OVRBoneId.Body_LeftHandIndexMetacarpal, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandIndexProximal] =          new JointInfo(OVRBoneId.Body_LeftHandIndexProximal, OVRBoneId.Body_LeftHandIndexMetacarpal),
            [BodyJointId.Body_LeftHandIndexIntermediate] =      new JointInfo(OVRBoneId.Body_LeftHandIndexIntermediate, OVRBoneId.Body_LeftHandIndexProximal),
            [BodyJointId.Body_LeftHandIndexDistal] =            new JointInfo(OVRBoneId.Body_LeftHandIndexDistal, OVRBoneId.Body_LeftHandIndexIntermediate),
            [BodyJointId.Body_LeftHandIndexTip] =               new JointInfo(OVRBoneId.Body_LeftHandIndexTip, OVRBoneId.Body_LeftHandIndexDistal),
            [BodyJointId.Body_LeftHandMiddleMetacarpal] =       new JointInfo(OVRBoneId.Body_LeftHandMiddleMetacarpal, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandMiddleProximal] =         new JointInfo(OVRBoneId.Body_LeftHandMiddleProximal, OVRBoneId.Body_LeftHandMiddleMetacarpal),
            [BodyJointId.Body_LeftHandMiddleIntermediate] =     new JointInfo(OVRBoneId.Body_LeftHandMiddleIntermediate, OVRBoneId.Body_LeftHandMiddleProximal),
            [BodyJointId.Body_LeftHandMiddleDistal] =           new JointInfo(OVRBoneId.Body_LeftHandMiddleDistal, OVRBoneId.Body_LeftHandMiddleIntermediate),
            [BodyJointId.Body_LeftHandMiddleTip] =              new JointInfo(OVRBoneId.Body_LeftHandMiddleTip, OVRBoneId.Body_LeftHandMiddleDistal),
            [BodyJointId.Body_LeftHandRingMetacarpal] =         new JointInfo(OVRBoneId.Body_LeftHandRingMetacarpal, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandRingProximal] =           new JointInfo(OVRBoneId.Body_LeftHandRingProximal, OVRBoneId.Body_LeftHandRingMetacarpal),
            [BodyJointId.Body_LeftHandRingIntermediate] =       new JointInfo(OVRBoneId.Body_LeftHandRingIntermediate, OVRBoneId.Body_LeftHandRingProximal),
            [BodyJointId.Body_LeftHandRingDistal] =             new JointInfo(OVRBoneId.Body_LeftHandRingDistal, OVRBoneId.Body_LeftHandRingIntermediate),
            [BodyJointId.Body_LeftHandRingTip] =                new JointInfo(OVRBoneId.Body_LeftHandRingTip, OVRBoneId.Body_LeftHandRingDistal),
            [BodyJointId.Body_LeftHandLittleMetacarpal] =       new JointInfo(OVRBoneId.Body_LeftHandLittleMetacarpal, OVRBoneId.Body_LeftHandWrist),
            [BodyJointId.Body_LeftHandLittleProximal] =         new JointInfo(OVRBoneId.Body_LeftHandLittleProximal, OVRBoneId.Body_LeftHandLittleMetacarpal),
            [BodyJointId.Body_LeftHandLittleIntermediate] =     new JointInfo(OVRBoneId.Body_LeftHandLittleIntermediate, OVRBoneId.Body_LeftHandLittleProximal),
            [BodyJointId.Body_LeftHandLittleDistal] =           new JointInfo(OVRBoneId.Body_LeftHandLittleDistal, OVRBoneId.Body_LeftHandLittleIntermediate),
            [BodyJointId.Body_LeftHandLittleTip] =              new JointInfo(OVRBoneId.Body_LeftHandLittleTip, OVRBoneId.Body_LeftHandLittleDistal),

            // Right Hand
            [BodyJointId.Body_RightHandWrist] =                 new JointInfo(OVRBoneId.Body_RightHandWrist, OVRBoneId.Body_RightArmLower),
            [BodyJointId.Body_RightHandPalm] =                  new JointInfo(OVRBoneId.Body_RightHandPalm, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandThumbMetacarpal] =       new JointInfo(OVRBoneId.Body_RightHandThumbMetacarpal, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandThumbProximal] =         new JointInfo(OVRBoneId.Body_RightHandThumbProximal, OVRBoneId.Body_RightHandThumbMetacarpal),
            [BodyJointId.Body_RightHandThumbDistal] =           new JointInfo(OVRBoneId.Body_RightHandThumbDistal, OVRBoneId.Body_RightHandThumbProximal),
            [BodyJointId.Body_RightHandThumbTip] =              new JointInfo(OVRBoneId.Body_RightHandThumbTip, OVRBoneId.Body_RightHandThumbDistal),
            [BodyJointId.Body_RightHandIndexMetacarpal] =       new JointInfo(OVRBoneId.Body_RightHandIndexMetacarpal, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandIndexProximal] =         new JointInfo(OVRBoneId.Body_RightHandIndexProximal, OVRBoneId.Body_RightHandIndexMetacarpal),
            [BodyJointId.Body_RightHandIndexIntermediate] =     new JointInfo(OVRBoneId.Body_RightHandIndexIntermediate, OVRBoneId.Body_RightHandIndexProximal),
            [BodyJointId.Body_RightHandIndexDistal] =           new JointInfo(OVRBoneId.Body_RightHandIndexDistal, OVRBoneId.Body_RightHandIndexIntermediate),
            [BodyJointId.Body_RightHandIndexTip] =              new JointInfo(OVRBoneId.Body_RightHandIndexTip, OVRBoneId.Body_RightHandIndexDistal),
            [BodyJointId.Body_RightHandMiddleMetacarpal] =      new JointInfo(OVRBoneId.Body_RightHandMiddleMetacarpal, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandMiddleProximal] =        new JointInfo(OVRBoneId.Body_RightHandMiddleProximal, OVRBoneId.Body_RightHandMiddleMetacarpal),
            [BodyJointId.Body_RightHandMiddleIntermediate] =    new JointInfo(OVRBoneId.Body_RightHandMiddleIntermediate, OVRBoneId.Body_RightHandMiddleProximal),
            [BodyJointId.Body_RightHandMiddleDistal] =          new JointInfo(OVRBoneId.Body_RightHandMiddleDistal, OVRBoneId.Body_RightHandMiddleIntermediate),
            [BodyJointId.Body_RightHandMiddleTip] =             new JointInfo(OVRBoneId.Body_RightHandMiddleTip, OVRBoneId.Body_RightHandMiddleDistal),
            [BodyJointId.Body_RightHandRingMetacarpal] =        new JointInfo(OVRBoneId.Body_RightHandRingMetacarpal, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandRingProximal] =          new JointInfo(OVRBoneId.Body_RightHandRingProximal, OVRBoneId.Body_RightHandRingMetacarpal),
            [BodyJointId.Body_RightHandRingIntermediate] =      new JointInfo(OVRBoneId.Body_RightHandRingIntermediate, OVRBoneId.Body_RightHandRingProximal),
            [BodyJointId.Body_RightHandRingDistal] =            new JointInfo(OVRBoneId.Body_RightHandRingDistal, OVRBoneId.Body_RightHandRingIntermediate),
            [BodyJointId.Body_RightHandRingTip] =               new JointInfo(OVRBoneId.Body_RightHandRingTip, OVRBoneId.Body_RightHandRingDistal),
            [BodyJointId.Body_RightHandLittleMetacarpal] =      new JointInfo(OVRBoneId.Body_RightHandLittleMetacarpal, OVRBoneId.Body_RightHandWrist),
            [BodyJointId.Body_RightHandLittleProximal] =        new JointInfo(OVRBoneId.Body_RightHandLittleProximal, OVRBoneId.Body_RightHandLittleMetacarpal),
            [BodyJointId.Body_RightHandLittleIntermediate] =    new JointInfo(OVRBoneId.Body_RightHandLittleIntermediate, OVRBoneId.Body_RightHandLittleProximal),
            [BodyJointId.Body_RightHandLittleDistal] =          new JointInfo(OVRBoneId.Body_RightHandLittleDistal, OVRBoneId.Body_RightHandLittleIntermediate),
            [BodyJointId.Body_RightHandLittleTip] =             new JointInfo(OVRBoneId.Body_RightHandLittleTip, OVRBoneId.Body_RightHandLittleDistal),
        };
    }
}
