
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
using UnityEditor;
using UnityEditor.SceneManagement;
using BoneId = OVRSkeleton.BoneId;

[CustomEditor(typeof(OVRCustomSkeleton))]
public class OVRCustomSkeletonEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawPropertiesExcluding(serializedObject, new string[] { "_customBones" });
		serializedObject.ApplyModifiedProperties();

		OVRCustomSkeleton skeleton = (OVRCustomSkeleton)target;
		OVRSkeleton.SkeletonType skeletonType = skeleton.GetSkeletonType();

		if (skeletonType == OVRSkeleton.SkeletonType.None)
		{
			EditorGUILayout.HelpBox("Please select a SkeletonType.", MessageType.Warning);
		}
		else
		{
			if (GUILayout.Button("Auto Map Bones"))
			{
				skeleton.TryAutoMapBonesByName();
				EditorUtility.SetDirty(skeleton);
				EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);
			}

			EditorGUILayout.LabelField("Bones", EditorStyles.boldLabel);
			BoneId start = skeleton.GetCurrentStartBoneId();
			BoneId end = skeleton.GetCurrentEndBoneId();
			if (start != BoneId.Invalid && end != BoneId.Invalid)
			{
				for (int i = (int)start; i < (int)end; ++i)
				{
					string boneName = OVRSkeleton.BoneLabelFromBoneId(skeletonType, (BoneId)i);
					skeleton.CustomBones[i] = (Transform)EditorGUILayout.ObjectField(boneName, skeleton.CustomBones[i], typeof(Transform), true);
				}
			}
		}
	}
}

