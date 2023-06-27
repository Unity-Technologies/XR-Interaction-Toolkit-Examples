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
    /// Skeleton meta data class, associated with each HumanyBodyBone enum.
    /// </summary>
    protected class OVRSkeletonMetadata
    {
        /// <summary>
        /// Data associated per bone.
        /// </summary>
        public class BoneData
        {
            /// <summary>
            /// Default, no-argument constructor.
            /// </summary>
            public BoneData()
            {
            }

            /// <summary>
            /// Copy constructor.
            /// </summary>
            /// <param name="otherBoneData">Other bone data to copy from.</param>
            public BoneData(BoneData otherBoneData)
            {
                OriginalJoint = otherBoneData.OriginalJoint;
                FromPosition = otherBoneData.FromPosition;
                ToPosition = otherBoneData.ToPosition;
                JointPairStart = otherBoneData.JointPairStart;
                JointPairEnd = otherBoneData.JointPairEnd;
                JointPairOrientation = otherBoneData.JointPairOrientation;
                CorrectionQuaternion = otherBoneData.CorrectionQuaternion;
                ParentTransform = otherBoneData.ParentTransform;
                DegenerateJoint = otherBoneData.DegenerateJoint;
            }

            /// <summary>
            /// Transform associated with joint.
            /// </summary>
            public Transform OriginalJoint;

            /// <summary>
            /// From position for joint pair, for debugging.
            /// </summary>
            public Vector3 FromPosition;

            /// <summary>
            /// To position for joint pair, for debugging.
            /// </summary>
            public Vector3 ToPosition;

            /// <summary>
            /// Start of joint pair (usually the original joint).
            /// </summary>
            public Transform JointPairStart;

            /// <summary>
            /// End of joint pair.
            /// </summary>
            public Transform JointPairEnd;

            /// <summary>
            /// Orientation or rotation corresponding to joint pair.
            /// If multiplied by forward, produces a coordinate axis.
            /// </summary>
            public Quaternion JointPairOrientation;

            /// <summary>
            /// Offset quaternion, used for retargeting rotations.
            /// </summary>
            public Quaternion? CorrectionQuaternion;

            /// <summary>
            /// Parent transform of joint. This is defined in a special way for OVRSkeleton,
            /// so we have to cache it ahead of time.
            /// </summary>
            public Transform ParentTransform;

            /// <summary>
            /// Some joints made have bad orientations due to faulty joint pairs.
            /// </summary>
            public bool DegenerateJoint = false;
        }

        /// <summary>
        /// Human body bone enum to bone data mapping.
        /// </summary>
        public Dictionary<HumanBodyBones, BoneData> BodyToBoneData { get; } =
            new Dictionary<HumanBodyBones, BoneData>();

        private readonly HumanBodyBones[] _boneEnumValues =
            (HumanBodyBones[])Enum.GetValues(typeof(HumanBodyBones));

        /// <summary>
        /// Constructor that copies another meta data class.
        /// </summary>
        /// <param name="otherSkeletonMetaData">Other meta data to copy from.</param>
        public OVRSkeletonMetadata(OVRSkeletonMetadata otherSkeletonMetaData)
        {
            BodyToBoneData = new Dictionary<HumanBodyBones, BoneData>();
            foreach (var key in otherSkeletonMetaData.BodyToBoneData.Keys)
            {
                var value = otherSkeletonMetaData.BodyToBoneData[key];
                BodyToBoneData[key] = new BoneData(value);
            }
        }

        /// <summary>
        /// Main constructor.
        /// </summary>
        /// <param name="animator">Animator to build meta data from.</param>
        public OVRSkeletonMetadata(Animator animator)
        {
            BuildBoneData(animator);
        }

        /// <summary>
        /// Constructor for OVRSkeleton.
        /// </summary>
        /// <param name="skeleton">Skeleton to build meta data from.</param>
        /// <param name="useBindPose">Whether to use bind pose (T-pose) or not.</param>
        /// <param name="customBoneIdToHumanBodyBone">Custom bone ID to human body bone mapping.</param>
        public OVRSkeletonMetadata(OVRSkeleton skeleton, bool useBindPose,
            Dictionary<OVRSkeleton.BoneId, HumanBodyBones> customBoneIdToHumanBodyBone)
        {
            BuildBoneDataSkeleton(skeleton, useBindPose, customBoneIdToHumanBodyBone);
        }


        /// <summary>
        /// Builds body to bone data with the OVRSkeleton.
        /// </summary>
        /// <param name="skeleton">The OVRSkeleton.</param>
        /// <param name="useBindPose">If true, use the bind pose.</param>
        /// <param name="customBoneIdToHumanBodyBone">Custom bone ID to human body bone mapping.</param>
        public void BuildBoneDataSkeleton(OVRSkeleton skeleton, bool useBindPose,
            Dictionary<OVRSkeleton.BoneId, HumanBodyBones> customBoneIdToHumanBodyBone)
        {
            AssembleSkeleton(skeleton, useBindPose, customBoneIdToHumanBodyBone);
        }


        private void AssembleSkeleton(OVRSkeleton skeleton, bool useBindPose,
            Dictionary<OVRSkeleton.BoneId, HumanBodyBones> customBoneIdToHumanBodyBone
            )
        {
            if (BodyToBoneData.Count != 0)
            {
                BodyToBoneData.Clear();
            }

            var allBones = useBindPose ? skeleton.BindPoses : skeleton.Bones;
            for (var i = 0; i < allBones.Count; i++)
            {
                var bone = allBones[i];

                if (!customBoneIdToHumanBodyBone.ContainsKey(bone.Id))
                {
                    continue;
                }

                var humanBodyBone = customBoneIdToHumanBodyBone[bone.Id];

                var boneData = new BoneData();
                boneData.OriginalJoint = bone.Transform;

                if (!OVRHumanBodyBonesMappings.BoneIdToJointPair.ContainsKey(bone.Id))
                {
                    Debug.LogError($"Can't find {bone.Id} in bone Id to joint pair map!");
                    continue;
                }

                var jointPair =
                    OVRHumanBodyBonesMappings.BoneIdToJointPair[bone.Id];
                var startOfPair = jointPair.Item1;
                var endofPair = jointPair.Item2;

                // for some tip transforms, the start of pair starts with one joint before
                boneData.JointPairStart = (startOfPair == bone.Id)
                    ? bone.Transform
                    : FindBoneWithBoneId(allBones, startOfPair).Transform;
                boneData.JointPairEnd = endofPair != OVRSkeleton.BoneId.Invalid
                    ? FindBoneWithBoneId(allBones, endofPair).Transform
                    : boneData.JointPairStart;
                boneData.ParentTransform = allBones[bone.ParentBoneIndex].Transform;

                if (boneData.JointPairStart == null)
                {
                    Debug.LogWarning($"{bone.Id} has invalid start joint.");
                }

                if (boneData.JointPairEnd == null)
                {
                    Debug.LogWarning($"{bone.Id} has invalid end joint.");
                }

                BodyToBoneData.Add(humanBodyBone, boneData);
            }
        }

        private static OVRBone FindBoneWithBoneId(IList<OVRBone> bones, OVRSkeleton.BoneId boneId)
        {
            for (var i = 0; i < bones.Count; i++)
            {
                if (bones[i].Id == boneId)
                {
                    return bones[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Builds body to bone data with an Animator.
        /// </summary>
        /// <param name="animator"></param>
        private void BuildBoneData(Animator animator)
        {
            if (BodyToBoneData.Count != 0)
            {
                BodyToBoneData.Clear();
            }

            foreach (var humanBodyBone in _boneEnumValues)
            {
                if (humanBodyBone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                var currTransform = animator.GetBoneTransform(humanBodyBone);
                if (currTransform == null)
                {
                    continue;
                }

                var boneData = new BoneData();
                boneData.OriginalJoint = currTransform;
                BodyToBoneData.Add(humanBodyBone, boneData);
            }

            // Find paired joints after all transforms have been tracked.
            // A joint start starts from a joints, ends at its child joint, and serves
            // as the "axis" of the joint.
            foreach (var key in BodyToBoneData.Keys)
            {
                var boneData = BodyToBoneData[key];
                var jointPair = OVRHumanBodyBonesMappings.BoneToJointPair[key];

                boneData.JointPairStart = jointPair.Item1 != HumanBodyBones.LastBone
                    ? animator.GetBoneTransform(jointPair.Item1)
                    : boneData.OriginalJoint;

                boneData.JointPairEnd = jointPair.Item2 != HumanBodyBones.LastBone
                    ? animator.GetBoneTransform(jointPair.Item2)
                    : FindFirstChild(boneData.OriginalJoint, boneData.OriginalJoint);

                boneData.ParentTransform = boneData.OriginalJoint.parent;

                if (boneData.JointPairStart == null)
                {
                    Debug.LogWarning($"{key} has invalid start joint.");
                }

                if (boneData.JointPairEnd == null)
                {
                    Debug.LogWarning($"{key} has invalid end joint.");
                }
            }
        }

        /// <summary>
        /// Builds coordinate axes for all bones.
        /// </summary>
        public void BuildCoordinateAxesForAllBones()
        {
            foreach (var key in BodyToBoneData.Keys)
            {
                var boneData = BodyToBoneData[key];
                var jointPairStartPosition = boneData.JointPairStart.position;
                Vector3 jointPairEndPosition;

                // Edge case: joint pair end is null or same node. If that's the case,
                // make joint pair end follow the axis from previous node.
                if (boneData.JointPairEnd == null ||
                    boneData.JointPairEnd == boneData.JointPairStart ||
                    (boneData.JointPairEnd.position - boneData.JointPairStart.position).magnitude <
                    Mathf.Epsilon)
                {
                    var node1 = boneData.ParentTransform;
                    var node2 = boneData.JointPairStart;
                    jointPairStartPosition = node1.position;
                    jointPairEndPosition = node2.position;
                    boneData.DegenerateJoint = true;
                }
                else
                {
                    jointPairEndPosition = boneData.JointPairEnd.position;
                    boneData.DegenerateJoint = false;
                }

                // with some joints like hands, fix the right vector. that's because the hand is nice and
                // flat, and the right vector should point to a thumb bone.
                if (key == HumanBodyBones.LeftHand || key == HumanBodyBones.RightHand)
                {
                    var jointToCreateRightVecWith = key == HumanBodyBones.LeftHand
                        ? HumanBodyBones.LeftThumbIntermediate
                        : HumanBodyBones.RightThumbIntermediate;
                    var rightVec = BodyToBoneData[jointToCreateRightVecWith].OriginalJoint.position -
                                   jointPairStartPosition;
                    boneData.JointPairOrientation =
                        CreateQuaternionForBoneDataWithRightVec(
                            jointPairStartPosition,
                            jointPairEndPosition,
                            rightVec);
                }
                else
                {
                    boneData.JointPairOrientation =
                        CreateQuaternionForBoneData(
                            jointPairStartPosition,
                            jointPairEndPosition);
                }

                var position = boneData.OriginalJoint.position;
                boneData.FromPosition = position;
                boneData.ToPosition = position +
                                      (jointPairEndPosition - jointPairStartPosition);
            }
        }

        private static Transform FindFirstChild(
            Transform startTransform,
            Transform currTransform)
        {
            if (startTransform != currTransform)
            {
                return currTransform;
            }

            // Dead end.
            if (currTransform.childCount == 0)
            {
                return null;
            }

            Transform foundChild = null;
            for (var i = 0; i < currTransform.childCount; i++)
            {
                var currChild = FindFirstChild(startTransform,
                    currTransform.GetChild(i));
                if (currChild != null)
                {
                    foundChild = currChild;
                    break;
                }
            }

            return foundChild;
        }

        private static Quaternion CreateQuaternionForBoneDataWithRightVec(
            Vector3 fromPosition,
            Vector3 toPosition,
            Vector3 rightVector)
        {
            var forwardVec = (toPosition - fromPosition).normalized;
            if (forwardVec.sqrMagnitude < Mathf.Epsilon)
            {
                forwardVec = Vector3.forward;
            }

            var upVector = Vector3.Cross(forwardVec, rightVector);
            return Quaternion.LookRotation(forwardVec, upVector);
        }

        private static Quaternion CreateQuaternionForBoneData(
            Vector3 fromPosition,
            Vector3 toPosition)
        {
            var forwardVec = (toPosition - fromPosition).normalized;

            if (forwardVec.sqrMagnitude < Mathf.Epsilon)
            {
                forwardVec = Vector3.forward;
            }

            return Quaternion.LookRotation(forwardVec);
        }
    }
}
