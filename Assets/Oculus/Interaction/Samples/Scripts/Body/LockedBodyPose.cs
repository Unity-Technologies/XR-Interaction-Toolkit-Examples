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
using UnityEngine;
using Oculus.Interaction.Body.Input;
using Oculus.Interaction.Body.PoseDetection;
using System.Collections.Generic;

namespace Oculus.Interaction.Body.Samples
{
    public class LockedBodyPose : MonoBehaviour, IBodyPose
    {
        private static readonly Pose HIP_OFFSET = new Pose()
        {
            position = new Vector3(0f, 0.923987f, 0f),
            rotation = Quaternion.Euler(0, 270, 270),
        };

        public event Action WhenBodyPoseUpdated = delegate { };

        [Tooltip("The body pose to be locked")]
        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _pose;
        private IBodyPose Pose;

        [Tooltip("The body pose will be locked relative to this " +
            "joint at the specified offset.")]
        [SerializeField]
        private BodyJointId _referenceJoint = BodyJointId.Body_Hips;

        [Tooltip("The reference joint will be placed at " +
            "this offset from the root.")]
        [SerializeField]
        private Pose _referenceOffset = HIP_OFFSET;

        protected bool _started = false;

        private Dictionary<BodyJointId, Pose> _lockedPoses;

        public ISkeletonMapping SkeletonMapping => Pose.SkeletonMapping;

        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            Pose.GetJointPoseLocal(bodyJointId, out pose);

        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            _lockedPoses.TryGetValue(bodyJointId, out pose);

        private void UpdateLockedBodyPose()
        {
            _lockedPoses.Clear();
            for (int i = 0; i < Constants.NUM_BODY_JOINTS; ++i)
            {
                BodyJointId jointId = (BodyJointId)i;
                if (Pose.GetJointPoseFromRoot(_referenceJoint, out Pose referencePose) &&
                    Pose.GetJointPoseFromRoot(jointId, out Pose jointPose))
                {
                    ref Pose offset = ref referencePose;
                    PoseUtils.Invert(ref offset);
                    PoseUtils.Multiply(offset, jointPose, ref jointPose);
                    PoseUtils.Multiply(_referenceOffset, jointPose, ref jointPose);
                    _lockedPoses[jointId] = jointPose;
                }
            }
            WhenBodyPoseUpdated.Invoke();
        }

        protected virtual void Awake()
        {
            _lockedPoses = new Dictionary<BodyJointId, Pose>();
            Pose = _pose as IBodyPose;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Pose, nameof(Pose));
            UpdateLockedBodyPose();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pose.WhenBodyPoseUpdated += UpdateLockedBodyPose;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pose.WhenBodyPoseUpdated -= UpdateLockedBodyPose;
            }
        }
    }
}
