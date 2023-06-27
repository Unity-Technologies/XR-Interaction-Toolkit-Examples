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

namespace Oculus.Interaction.Body.Samples
{
    public class BodyPoseSwitcher : MonoBehaviour, IBodyPose
    {
        public enum PoseSource
        {
            PoseA,
            PoseB,
        }

        public event Action WhenBodyPoseUpdated = delegate { };

        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _poseA;
        private IBodyPose PoseA;

        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _poseB;
        private IBodyPose PoseB;

        [SerializeField]
        private PoseSource _source = PoseSource.PoseA;

        public ISkeletonMapping SkeletonMapping => GetPose().SkeletonMapping;

        public bool GetJointPoseFromRoot(BodyJointId bodyJointId, out Pose pose) =>
            GetPose().GetJointPoseFromRoot(bodyJointId, out pose);

        public bool GetJointPoseLocal(BodyJointId bodyJointId, out Pose pose) =>
            GetPose().GetJointPoseLocal(bodyJointId, out pose);

        protected bool _started = false;

        public PoseSource Source
        {
            get { return _source; }
            set
            {
                bool changed = value != _source;
                _source = value;
                if (changed)
                {
                    WhenBodyPoseUpdated.Invoke();
                }
            }
        }

        public void UsePoseA()
        {
            Source = PoseSource.PoseA;
        }

        public void UsePoseB()
        {
            Source = PoseSource.PoseB;
        }

        protected virtual void Awake()
        {
            PoseA = _poseA as IBodyPose;
            PoseB = _poseB as IBodyPose;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(PoseA, nameof(PoseA));
            this.AssertField(PoseB, nameof(PoseB));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                PoseA.WhenBodyPoseUpdated += () => OnPoseUpdated(PoseSource.PoseA);
                PoseB.WhenBodyPoseUpdated += () => OnPoseUpdated(PoseSource.PoseB);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                PoseA.WhenBodyPoseUpdated -= () => OnPoseUpdated(PoseSource.PoseA);
                PoseB.WhenBodyPoseUpdated -= () => OnPoseUpdated(PoseSource.PoseB);
            }
        }

        private void OnPoseUpdated(PoseSource source)
        {
            if (source == Source)
            {
                WhenBodyPoseUpdated.Invoke();
            }
        }

        private IBodyPose GetPose()
        {
            switch (Source)
            {
                default:
                case PoseSource.PoseA:
                    return PoseA;
                case PoseSource.PoseB:
                    return PoseB;
            }
        }
    }
}
