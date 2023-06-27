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
using System;
using Oculus.Interaction.Input;

namespace Oculus.Interaction.Body.Input
{
    public class Body : DataModifier<BodyDataAsset>, IBody
    {
        [Tooltip("If assigned, joint pose translations into world " +
            "space will be performed via this transform. If unassigned, " +
            "world joint poses will be returned in tracking space.")]
        [SerializeField, Optional]
        private Transform _trackingSpace;

        public bool IsConnected => GetData().IsDataValid;

        public bool IsHighConfidence => GetData().IsDataHighConfidence;

        public float Scale => GetData().RootScale;

        public ISkeletonMapping SkeletonMapping => GetData().SkeletonMapping;

        public bool IsTrackedDataValid => GetData().IsDataValid;

        public event Action WhenBodyUpdated = delegate { };

        private BodyJointsCache _jointPosesCache;

        public bool GetJointPose(BodyJointId bodyJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!IsTrackedDataValid || !SkeletonMapping.Joints.Contains(bodyJointId))
            {
                return false;
            }

            CheckJointPosesCacheUpdate();
            pose = _jointPosesCache.GetWorldJointPose(bodyJointId);
            return true;
        }

        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!IsTrackedDataValid || !SkeletonMapping.Joints.Contains(bodyJointId))
            {
                return false;
            }

            CheckJointPosesCacheUpdate();
            pose = _jointPosesCache.GetLocalJointPose(bodyJointId);
            return true;
        }

        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose)
        {
            pose = Pose.identity;
            if (!IsTrackedDataValid || !SkeletonMapping.Joints.Contains(bodyJointId))
            {
                return false;
            }

            CheckJointPosesCacheUpdate();
            pose = _jointPosesCache.GetJointPoseFromRoot(bodyJointId);
            return true;
        }

        public bool GetRootPose(out Pose pose)
        {
            pose = Pose.identity;
            if (!IsTrackedDataValid)
            {
                return false;
            }

            CheckJointPosesCacheUpdate();
            pose = _jointPosesCache.GetWorldRootPose();
            return true;
        }

        private void InitializeJointPosesCache()
        {
            if (_jointPosesCache == null)
            {
                _jointPosesCache = new BodyJointsCache();
            }
        }

        private void CheckJointPosesCacheUpdate()
        {
            if (_jointPosesCache != null
                && CurrentDataVersion != _jointPosesCache.LocalDataVersion)
            {
                _jointPosesCache.Update(GetData(), CurrentDataVersion, _trackingSpace);
            }
        }

        protected override void Apply(BodyDataAsset data)
        {
            // Default implementation does nothing, to allow instantiation of this modifier directly
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();
            if (Started)
            {
                InitializeJointPosesCache();
                WhenBodyUpdated.Invoke();
            }
        }
    }
}
