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
using UnityEngine;

public class OVRCustomSkeleton : OVRSkeleton, ISerializationCallbackReceiver
{
    [HideInInspector] [SerializeField] private List<Transform> _customBones_V2;
    public List<Transform> CustomBones => _customBones_V2;

    /// <summary>
    /// List of skeleton structures to be retargeted to the supported format for body tracking.
    /// </summary>
    public enum RetargetingType
    {
        /// <summary>The default skeleton structure of the Oculus tracking system</summary>
        OculusSkeleton
    }

    [SerializeField, HideInInspector]
    internal RetargetingType retargetingType = RetargetingType.OculusSkeleton;

    protected override Transform GetBoneTransform(BoneId boneId) => _customBones_V2[(int)boneId];

#if UNITY_EDITOR
    private bool _shouldSetDirty;

    private void OnValidate()
    {
        if (!_shouldSetDirty) return;

        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        UnityEditor.EditorUtility.SetDirty(this);
        _shouldSetDirty = false;
    }
#endif

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        AllocateBones();
    }

    private void AllocateBones()
    {
        if (_customBones_V2.Count == (int)BoneId.Max) return;

        // Make sure we have the right number of bones
        while (_customBones_V2.Count < (int)BoneId.Max)
        {
            _customBones_V2.Add(null);
        }

#if UNITY_EDITOR
        _shouldSetDirty = true;
#endif
    }

    internal void SetSkeletonType(SkeletonType skeletonType)
    {
        _skeletonType = skeletonType;
        _customBones_V2 ??= new List<Transform>();

        AllocateBones();
    }
}
