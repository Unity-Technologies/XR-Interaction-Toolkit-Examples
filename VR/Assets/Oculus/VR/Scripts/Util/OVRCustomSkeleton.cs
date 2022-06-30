/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(-80)]
public class OVRCustomSkeleton : OVRSkeleton
{
	[SerializeField]
	private bool _applyBoneTranslations = true;

	[HideInInspector]
	[SerializeField]
	private List<Transform> _customBones_V2 = new List<Transform>(new Transform[(int)BoneId.Max]);

#if UNITY_EDITOR

	private static readonly string[] _fbxHandSidePrefix = { "l_", "r_" };
	private static readonly string _fbxHandBonePrefix = "b_";

	private static readonly string[] _fbxHandBoneNames =
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

	private static readonly string[] _fbxHandFingerNames =
	{
		"thumb",
		"index",
		"middle",
		"ring",
		"pinky"
	};
#endif

	public List<Transform> CustomBones { get { return _customBones_V2; } }

#if UNITY_EDITOR
	public void TryAutoMapBonesByName()
	{
		BoneId start = GetCurrentStartBoneId();
		BoneId end = GetCurrentEndBoneId();
		SkeletonType skeletonType = GetSkeletonType();
		if (start != BoneId.Invalid && end != BoneId.Invalid)
		{
			for (int bi = (int)start; bi < (int)end; ++bi)
			{
				string fbxBoneName = FbxBoneNameFromBoneId(skeletonType, (BoneId)bi);
				Transform t = transform.FindChildRecursive(fbxBoneName);

				if (t != null)
				{
					_customBones_V2[(int)bi] = t;
				}
			}
		}
	}

	private static string FbxBoneNameFromBoneId(SkeletonType skeletonType, BoneId bi)
	{
		{
			if (bi >= BoneId.Hand_ThumbTip && bi <= BoneId.Hand_PinkyTip)
			{
				return _fbxHandSidePrefix[(int)skeletonType] + _fbxHandFingerNames[(int)bi - (int)BoneId.Hand_ThumbTip] + "_finger_tip_marker";
			}
			else
			{
				return _fbxHandBonePrefix + _fbxHandSidePrefix[(int)skeletonType] + _fbxHandBoneNames[(int)bi];
			}
		}
	}
#endif

	protected override void InitializeBones()
	{
		bool flipX = (_skeletonType == SkeletonType.HandLeft || _skeletonType == SkeletonType.HandRight);

		if (_bones == null || _bones.Count != _skeleton.NumBones)
		{
			_bones = new List<OVRBone>(new OVRBone[_skeleton.NumBones]);
			Bones = _bones.AsReadOnly();
		}

		for (int i = 0; i < _bones.Count; ++i)
		{
			OVRBone bone = _bones[i] ?? (_bones[i] = new OVRBone());
			bone.Id = (OVRSkeleton.BoneId)_skeleton.Bones[i].Id;
			bone.ParentBoneIndex = _skeleton.Bones[i].ParentBoneIndex;
			bone.Transform = _customBones_V2[(int)bone.Id];

			if (_applyBoneTranslations)
			{
				bone.Transform.localPosition = flipX ? _skeleton.Bones[i].Pose.Position.FromFlippedXVector3f() : _skeleton.Bones[i].Pose.Position.FromFlippedZVector3f();
			}

			bone.Transform.localRotation = flipX ? _skeleton.Bones[i].Pose.Orientation.FromFlippedXQuatf() : _skeleton.Bones[i].Pose.Orientation.FromFlippedZQuatf();
		}
	}
}
