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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class HandDebugVisual : MonoBehaviour, IHandVisual
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand => _hand as IHand;

        public bool ForceOffVisibility { get; set; }

        public bool IsVisible => _isVisible;

        public event Action WhenHandVisualUpdated = delegate { };

        private bool _isVisible = true;
        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += UpdateSkeleton;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started && _hand != null)
            {
                Hand.WhenHandUpdated -= UpdateSkeleton;
            }
        }

        protected virtual void LateUpdate()
        {
            if (Hand == null || !IsVisible)
            {
                return;
            }

            Draw();
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying || Hand == null ||
                !isActiveAndEnabled || !IsVisible)
            {
                return;
            }

            Draw();
        }

        private void Draw()
        {
            DebugGizmos.Color = Color.white;

            for (var i = 0; i < Constants.NUM_HAND_JOINTS; ++i)
            {
                HandJointId jointId = (HandJointId)i;
                if (Hand.GetJointPose(jointId, out Pose jointPose))
                {
                    // Draw Joint
                    DebugGizmos.LineWidth = 0.015f;
                    DebugGizmos.DrawLine(jointPose.position, jointPose.position);

                    HandJointId parentJoint = HandJointUtils.JointParentList[i];
                    if (parentJoint != HandJointId.Invalid &&
                        Hand.GetJointPose(parentJoint, out Pose parentPose))
                    {
                        // Draw Bone
                        DebugGizmos.LineWidth = 0.01f;
                        DebugGizmos.DrawLine(jointPose.position, parentPose.position);
                    }
                }
            }
        }

        public void UpdateSkeleton()
        {
            if (!Hand.IsTrackedDataValid)
            {
                if (IsVisible || ForceOffVisibility)
                {
                    _isVisible = false;
                }
                WhenHandVisualUpdated.Invoke();
                return;
            }

            if (!IsVisible && !ForceOffVisibility)
            {
                _isVisible = true;
            }
            else if (IsVisible && ForceOffVisibility)
            {
                _isVisible = false;
            }

            WhenHandVisualUpdated.Invoke();
        }

        public Pose GetJointPose(HandJointId jointId, Space space)
        {
            if (space == Space.Self)
            {
                if (Hand.GetJointPoseLocal(jointId, out Pose pose))
                {
                    return pose;
                }
            }
            else if (space == Space.World)
            {
                if (Hand.GetJointPose(jointId, out Pose pose))
                {
                    return pose;
                }
            }
            return new Pose();
        }
    }
}
