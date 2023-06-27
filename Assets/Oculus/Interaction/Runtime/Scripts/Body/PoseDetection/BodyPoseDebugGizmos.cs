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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Body.PoseDetection
{
    public class BodyPoseDebugGizmos : SkeletonDebugGizmos
    {
        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _bodyPose;
        private IBodyPose BodyPose;

        protected virtual void Awake()
        {
            BodyPose = _bodyPose as IBodyPose;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(BodyPose);
        }

        protected virtual void Update()
        {
            foreach (BodyJointId joint in BodyPose.SkeletonMapping.Joints)
            {
                Draw(joint, GetVisibilityFlags());
            }
        }

        private VisibilityFlags GetVisibilityFlags()
        {
            VisibilityFlags modifiedFlags = Visibility;
            if (HasNegativeScale)
            {
                modifiedFlags &= ~VisibilityFlags.Axes;
            }
            return modifiedFlags;
        }

        protected override bool TryGetWorldJointPose(BodyJointId jointId, out Pose pose)
        {
            if (BodyPose.GetJointPoseFromRoot(jointId, out pose))
            {
                pose.position = transform.TransformPoint(pose.position);
                pose.rotation = transform.rotation * pose.rotation;
                return true;
            }
            return false;
        }

        protected override bool TryGetParentJointId(BodyJointId jointId, out BodyJointId parent)
        {
            return BodyPose.SkeletonMapping.TryGetParentJointId(jointId, out parent);
        }

        #region Inject

        public void InjectAllBodyJointDebugGizmos(IBodyPose bodyPose)
        {
            InjectBodyPose(bodyPose);
        }

        public void InjectBodyPose(IBodyPose bodyPose)
        {
            _bodyPose = bodyPose as UnityEngine.Object;
            BodyPose = bodyPose;
        }

        #endregion
    }
}
