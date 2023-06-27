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

using UnityEngine;
using Oculus.Interaction.Body.Input;

using IOVRSkeletonDataProvider = OVRSkeleton.IOVRSkeletonDataProvider;
using OVRBoneId = OVRPlugin.BoneId;

namespace Oculus.Interaction.Body.PoseDetection
{
    public class OVRBodyPoseSkeletonProvider : MonoBehaviour, IOVRSkeletonDataProvider
    {
        private const int OVR_NUM_JOINTS =
            OVRBoneId.Body_End - OVRBoneId.Body_Start;

        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _bodyPose;
        private IBodyPose BodyPose;

        private OVRPlugin.Quatf[] _boneRotations = new OVRPlugin.Quatf[OVR_NUM_JOINTS];
        private OVRPlugin.Vector3f[] _boneTranslations = new OVRPlugin.Vector3f[OVR_NUM_JOINTS];

        private readonly OVRSkeletonMapping _mapping = new OVRSkeletonMapping();

        protected virtual void Awake()
        {
            BodyPose = _bodyPose as IBodyPose;
        }

        protected virtual void Start()
        {
            this.AssertField(BodyPose, nameof(BodyPose));
        }

        OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
        {
            T[] EnsureLength<T>(T[] array, int length) => array?.Length == length ? array : new T[length];

            // Make sure arrays have been allocated
            _boneRotations = EnsureLength(_boneRotations, OVR_NUM_JOINTS);
            _boneTranslations = EnsureLength(_boneTranslations, OVR_NUM_JOINTS);

            // Copy joint poses into bone arrays
            for (int i = 0; i < OVR_NUM_JOINTS; ++i)
            {
                OVRBoneId boneId = (OVRBoneId)i;
                if (_mapping.TryGetBodyJointId(boneId, out BodyJointId jointId) &&
                    BodyPose.GetJointPoseFromRoot(jointId, out Pose pose))
                {
                    _boneRotations[i] = pose.rotation.ToFlippedZQuatf();
                    _boneTranslations[i] = pose.position.ToFlippedZVector3f();
                }
            }

            OVRPlugin.Posef rootPose;
            if (BodyPose.GetJointPoseFromRoot(BodyJointId.Body_Root, out Pose root))
            {
                rootPose = new OVRPlugin.Posef()
                {
                    Orientation = root.rotation.ToFlippedXQuatf(),
                    Position = root.position.ToFlippedZVector3f()
                };
            }
            else
            {
                rootPose = default;
            }

            return new OVRSkeleton.SkeletonPoseData
            {
                IsDataValid = true,
                IsDataHighConfidence = true,
                RootPose = rootPose,
                RootScale = 1.0f,
                BoneRotations = _boneRotations,
                BoneTranslations = _boneTranslations,
            };
        }

        public OVRSkeleton.SkeletonType GetSkeletonType()
        {
            return OVRSkeleton.SkeletonType.Body;
        }
    }
}
