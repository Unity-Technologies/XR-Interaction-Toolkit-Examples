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
using UnityEngine.Assertions;

public partial class OVRUnityHumanoidSkeletonRetargeter : OVRSkeleton
{
    private OVRSkeletonMetadata _sourceSkeletonData;
    protected OVRSkeletonMetadata SourceSkeletonData => _sourceSkeletonData;

    private OVRSkeletonMetadata _sourceSkeletonTPoseData;
    protected OVRSkeletonMetadata SourceSkeletonTPoseData => _sourceSkeletonTPoseData;

    private OVRSkeletonMetadata _targetSkeletonData;
    protected OVRSkeletonMetadata TargetSkeletonData => _targetSkeletonData;

    private Animator _animatorTargetSkeleton;
    protected Animator AnimatorTargetSkeleton => _animatorTargetSkeleton;

    private Dictionary<BoneId, HumanBodyBones> _customBoneIdToHumanBodyBone =
        new Dictionary<BoneId, HumanBodyBones>();

    protected Dictionary<BoneId, HumanBodyBones> CustomBoneIdToHumanBodyBone
    {
        get => _customBoneIdToHumanBodyBone;
    }

    private readonly Dictionary<HumanBodyBones, Quaternion> _targetTPoseRotations =
        new Dictionary<HumanBodyBones, Quaternion>();

    protected Dictionary<HumanBodyBones, Quaternion> TargetTPoseRotations
    {
        get => _targetTPoseRotations;
    }

    private int _lastSkelChangeCount = -1;

    protected class JointAdjustment
    {
        /// <summary>
        /// Joint to adjust.
        /// </summary>
        public HumanBodyBones Joint;

        /// <summary>
        /// Rotation to apply to the joint, post-retargeting.
        /// </summary>
        public Quaternion RotationChange = Quaternion.identity;

        /// <summary>
        /// Allows disable rotational transform on joint.
        /// </summary>
        public bool DisableRotationTransform = false;

        /// <summary>
        /// Allows disable position transform on joint.
        /// </summary>
        public bool DisablePositionTransform = false;

        /// <summary>
        /// Allows mapping this human body bone to OVRSkeleton bone different from the
        /// standard. An ignore value indicates to not override; remove means to exclude
        /// from retargeting. Cannot be changed at runtime.
        /// </summary>
        public OVRHumanBodyBonesMappings.BodyTrackingBoneId BoneIdOverrideValue =
            OVRHumanBodyBonesMappings.BodyTrackingBoneId.NoOverride;
    }

    public OVRUnityHumanoidSkeletonRetargeter()
    {
        _skeletonType = SkeletonType.Body;
    }

    private readonly JointAdjustment[] _adjustments =
    {
        new JointAdjustment
        {
            Joint = HumanBodyBones.Hips,
            RotationChange = Quaternion.Euler(60.0f, 0.0f, 0.0f)
        }
    };

    protected JointAdjustment[] Adjustments
    {
        get => _adjustments;
    }

    private readonly OVRHumanBodyBonesMappings.BodySection[] _bodySectionsToAlign =
    {
        OVRHumanBodyBonesMappings.BodySection.LeftArm, OVRHumanBodyBonesMappings.BodySection.RightArm,
        OVRHumanBodyBonesMappings.BodySection.LeftHand, OVRHumanBodyBonesMappings.BodySection.RightHand,
        OVRHumanBodyBonesMappings.BodySection.Hips, OVRHumanBodyBonesMappings.BodySection.Back,
        OVRHumanBodyBonesMappings.BodySection.Neck, OVRHumanBodyBonesMappings.BodySection.Head
    };

    protected OVRHumanBodyBonesMappings.BodySection[] BodySectionsToAlign
    {
        get => _bodySectionsToAlign;
    }

    private readonly OVRHumanBodyBonesMappings.BodySection[] _bodySectionToPosition =
    {
        OVRHumanBodyBonesMappings.BodySection.LeftArm, OVRHumanBodyBonesMappings.BodySection.RightArm,
        OVRHumanBodyBonesMappings.BodySection.LeftHand, OVRHumanBodyBonesMappings.BodySection.RightHand,
        OVRHumanBodyBonesMappings.BodySection.Hips, OVRHumanBodyBonesMappings.BodySection.Neck,
        OVRHumanBodyBonesMappings.BodySection.Head
    };

    protected OVRHumanBodyBonesMappings.BodySection[] BodySectionToPosition
    {
        get => _bodySectionToPosition;
    }

    protected override void Start()
    {
        base.Start();

        Assert.IsTrue(OVRSkeleton.IsBodySkeleton(_skeletonType));

        ValidateGameObjectForUnityHumanoidRetargeting(gameObject);
        _animatorTargetSkeleton = gameObject.GetComponent<Animator>();

        CreateCustomBoneIdToHumanBodyBoneMapping();
        StoreTTargetPoseRotations();

        _targetSkeletonData = new OVRSkeletonMetadata(_animatorTargetSkeleton);
        _targetSkeletonData.BuildCoordinateAxesForAllBones();
    }

    internal static void ValidateGameObjectForUnityHumanoidRetargeting(GameObject go)
    {
        if (go.GetComponent<Animator>() == null)
        {
            throw new InvalidOperationException(
                $"Retargeting to Unity Humanoid requires an {nameof(Animator)} component with a humanoid avatar on T-Pose");
        }
    }

    private void StoreTTargetPoseRotations()
    {
        for (var i = HumanBodyBones.Hips; i < HumanBodyBones.LastBone; i++)
        {
            var boneTransform = _animatorTargetSkeleton.GetBoneTransform(i);
            _targetTPoseRotations[i] = boneTransform ? boneTransform.rotation : Quaternion.identity;
        }
    }

    private void CreateCustomBoneIdToHumanBodyBoneMapping()
    {
        CopyBoneIdToHumanBodyBoneMapping();
        AdjustCustomBoneIdToHumanBodyBoneMapping();
    }

    private void CopyBoneIdToHumanBodyBoneMapping()
    {
        _customBoneIdToHumanBodyBone.Clear();
        foreach (var keyValuePair in OVRHumanBodyBonesMappings.BoneIdToHumanBodyBone)
        {
            _customBoneIdToHumanBodyBone.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    private void AdjustCustomBoneIdToHumanBodyBoneMapping()
    {
        // if there is a mapping override that the user provided,
        // enforce it.
        foreach (var adjustment in _adjustments)
        {
            if (adjustment.BoneIdOverrideValue == OVRHumanBodyBonesMappings.BodyTrackingBoneId.NoOverride)
            {
                continue;
            }
            if (adjustment.BoneIdOverrideValue == OVRHumanBodyBonesMappings.BodyTrackingBoneId.Remove)
            {
                RemoveMappingCorrespondingToHumanBodyBone(adjustment.Joint);
            }
            else
            {
                _customBoneIdToHumanBodyBone[(BoneId)adjustment.BoneIdOverrideValue]
                    = adjustment.Joint;
            }
        }
    }

    private void RemoveMappingCorrespondingToHumanBodyBone(HumanBodyBones boneId)
    {
        foreach (var key in _customBoneIdToHumanBodyBone.Keys)
        {
            if (_customBoneIdToHumanBodyBone[key] == boneId)
            {
                _customBoneIdToHumanBodyBone.Remove(key);
                return;
            }
        }
    }

    protected override void Update()
    {
        UpdateSkeleton();

        RecomputeSkeletalOffsetsIfNecessary();

        AlignTargetWithSource();
    }

    protected void RecomputeSkeletalOffsetsIfNecessary()
    {
        if (_lastSkelChangeCount != SkeletonChangedCount)
        {
            ComputeOffsetsUsingSkeletonComponent();
        }
    }

    private void ComputeOffsetsUsingSkeletonComponent()
    {
        if (!IsInitialized ||
            BindPoses == null || BindPoses.Count == 0)
        {
            return;
        }

        if (_sourceSkeletonData == null)
        {
            _sourceSkeletonData = new OVRSkeletonMetadata(this, false, _customBoneIdToHumanBodyBone
            );
        }
        else
        {
            _sourceSkeletonData.BuildBoneDataSkeleton(this, false, _customBoneIdToHumanBodyBone);
        }

        _sourceSkeletonData.BuildCoordinateAxesForAllBones();

        if (_sourceSkeletonTPoseData == null)
        {
            _sourceSkeletonTPoseData = new OVRSkeletonMetadata(this, true, _customBoneIdToHumanBodyBone
            );
        }
        else
        {
            _sourceSkeletonTPoseData.BuildBoneDataSkeleton(this, true, _customBoneIdToHumanBodyBone);
        }

        _sourceSkeletonTPoseData.BuildCoordinateAxesForAllBones();

        for (var i = 0; i < BindPoses.Count; i++)
        {
            if (!_customBoneIdToHumanBodyBone.TryGetValue(BindPoses[i].Id, out var humanBodyBone))
            {
                continue;
            }

            if (!_targetSkeletonData.BodyToBoneData.TryGetValue(humanBodyBone, out var targetData))
            {
                continue;
            }

            var bodySection = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];

            if (!IsBodySectionInArray(bodySection,
                    _bodySectionsToAlign
                ))
            {
                continue;
            }

            if (!_sourceSkeletonTPoseData.BodyToBoneData.TryGetValue(humanBodyBone, out var sourceTPoseData))
            {
                continue;
            }

            if (!_sourceSkeletonData.BodyToBoneData.TryGetValue(humanBodyBone, out var sourcePoseData))
            {
                continue;
            }

            // if encountered degenerate source bones, skip
            if (sourceTPoseData.DegenerateJoint || sourcePoseData.DegenerateJoint)
            {
                targetData.CorrectionQuaternion = null;
                continue;
            }

            var forwardSource = sourceTPoseData.JointPairOrientation * Vector3.forward;
            var forwardTarget = targetData.JointPairOrientation * Vector3.forward;
            var targetToSrc = Quaternion.FromToRotation(forwardTarget, forwardSource);

            var sourceRotationValueInv = Quaternion.Inverse(BindPoses[i].Transform.rotation);

            targetData.CorrectionQuaternion =
                sourceRotationValueInv * targetToSrc * _targetTPoseRotations[humanBodyBone];
        }

        _lastSkelChangeCount = SkeletonChangedCount;
    }

    protected static bool IsBodySectionInArray(
        OVRHumanBodyBonesMappings.BodySection bodySectionToCheck,
        OVRHumanBodyBonesMappings.BodySection[] sectionArrayToCheck)
    {
        foreach (var bodySection in sectionArrayToCheck)
        {
            if (bodySection == bodySectionToCheck)
            {
                return true;
            }
        }

        return false;
    }

    private void AlignTargetWithSource()
    {
        if (!IsInitialized || Bones == null || Bones.Count == 0)
        {
            return;
        }

        for (var i = 0; i < Bones.Count; i++)
        {
            if (!_customBoneIdToHumanBodyBone.TryGetValue(Bones[i].Id, out var humanBodyBone))
            {
                continue;
            }

            if (!_targetSkeletonData.BodyToBoneData.TryGetValue(humanBodyBone, out var targetData))
            {
                continue;
            }

            // Skip if we cannot map the joint at all.
            if (!targetData.CorrectionQuaternion.HasValue)
            {
                continue;
            }

            var targetJoint = targetData.OriginalJoint;
            var correctionQuaternion = targetData.CorrectionQuaternion.Value;
            var adjustment = FindAdjustment(humanBodyBone);

            var bodySectionOfJoint = OVRHumanBodyBonesMappings.BoneToBodySection[humanBodyBone];
            var shouldUpdatePosition = IsBodySectionInArray(
                bodySectionOfJoint,
                    _bodySectionToPosition
            );

            if (adjustment == null)
            {
                targetJoint.rotation = Bones[i].Transform.rotation * correctionQuaternion;
                if (shouldUpdatePosition)
                {
                    targetJoint.position = Bones[i].Transform.position;
                }
            }
            else
            {
                if (!adjustment.DisableRotationTransform)
                {
                    targetJoint.rotation = Bones[i].Transform.rotation * correctionQuaternion;
                }

                targetJoint.rotation *= adjustment.RotationChange;
                if (!adjustment.DisablePositionTransform && shouldUpdatePosition)
                {
                    targetJoint.position = Bones[i].Transform.position;
                }
            }
        }
    }

    protected JointAdjustment FindAdjustment(HumanBodyBones boneId)
    {
        foreach (var adjustment in _adjustments)
        {
            if (adjustment.Joint == boneId)
            {
                return adjustment;
            }
        }

        return null;
    }
}
