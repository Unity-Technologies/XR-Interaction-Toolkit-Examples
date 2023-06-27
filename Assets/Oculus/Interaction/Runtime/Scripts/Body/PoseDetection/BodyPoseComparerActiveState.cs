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
using Oculus.Interaction.Body.Input;

namespace Oculus.Interaction.Body.PoseDetection
{
    /// <summary>
    /// Compares a user-provided set of joints between two Body Poses.
    /// </summary>
    public class BodyPoseComparerActiveState :
        MonoBehaviour, IActiveState
    {
        public struct BodyPoseComparerFeatureState
        {
            public readonly float Delta;

            public readonly float MaxDelta;

            public BodyPoseComparerFeatureState(float delta, float maxDelta)
            {
                Delta = delta;
                MaxDelta = maxDelta;
            }
        }

        [Serializable]
        public class JointComparerConfig
        {
            [Tooltip("The joint to compare from each Body Pose")]
            public BodyJointId Joint = BodyJointId.Body_Head;

            [Min(0)]
            [Tooltip("The maximum angle that two joint rotations " +
                "can be from each other to be considered equal.")]
            public float MaxDelta = 30f;

            [Tooltip("The width of the threshold when transitioning " +
                "states. " + nameof(Width) + " / 2 is added to " +
                nameof(MaxDelta) + " when leaving Active state, and " +
                "subtracted when entering.")]
            [Min(0)]
            public float Width = 4f;
        }

        [Tooltip("The first body pose to compare.")]
        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _poseA;
        private IBodyPose PoseA;

        [Tooltip("The second body pose to compare.")]
        [SerializeField, Interface(typeof(IBodyPose))]
        private UnityEngine.Object _poseB;
        private IBodyPose PoseB;

        [SerializeField]
        private List<JointComparerConfig> _configs =
            new List<JointComparerConfig>()
            {
                new JointComparerConfig()
            };

        [Tooltip("A new state must be maintaned for at least this " +
            "many seconds before the Active property changes.")]
        [SerializeField]
        private float _minTimeInState = 0.05f;

        public float MinTimeInState
        {
            get => _minTimeInState;
            set => _minTimeInState = value;
        }

        public IReadOnlyDictionary<JointComparerConfig, BodyPoseComparerFeatureState> FeatureStates =>
            _featureStates;

        private Dictionary<JointComparerConfig, BodyPoseComparerFeatureState> _featureStates =
            new Dictionary<JointComparerConfig, BodyPoseComparerFeatureState>();

        private Func<float> _timeProvider;
        private bool _isActive;
        private bool _internalActive;
        private float _lastStateChangeTime;

        protected virtual void Awake()
        {
            PoseA = _poseA as IBodyPose;
            PoseB = _poseB as IBodyPose;
            _timeProvider = () => Time.time;
        }

        protected virtual void Start()
        {
            this.AssertField(PoseA, nameof(PoseA));
            this.AssertField(PoseB, nameof(PoseB));
        }

        public bool Active
        {
            get
            {
                if (!isActiveAndEnabled)
                {
                    return false;
                }

                bool wasActive = _internalActive;
                _internalActive = true;
                foreach (var config in _configs)
                {
                    float maxDelta = wasActive ?
                                     config.MaxDelta + config.Width / 2f :
                                     config.MaxDelta - config.Width / 2f;

                    bool withinDelta = GetJointDelta(config.Joint, out float delta) &&
                                       Mathf.Abs(delta) <= maxDelta;
                    _featureStates[config] = new BodyPoseComparerFeatureState(delta, maxDelta);
                    _internalActive &= withinDelta;
                }
                float time = _timeProvider();
                if (wasActive != _internalActive)
                {
                    _lastStateChangeTime = time;
                }
                if (time - _lastStateChangeTime >= _minTimeInState)
                {
                    _isActive = _internalActive;
                }
                return _isActive;
            }
        }

        private bool GetJointDelta(BodyJointId joint, out float delta)
        {
            if (!PoseA.GetJointPoseLocal(joint, out Pose localA) ||
                !PoseB.GetJointPoseLocal(joint, out Pose localB))
            {
                delta = 0;
                return false;
            }

            delta = Quaternion.Angle(localA.rotation, localB.rotation);
            return true;
        }

        #region Inject
        public void InjectAllBodyPoseComparerActiveState(
            IBodyPose poseA, IBodyPose poseB,
            IEnumerable<JointComparerConfig> configs)
        {
            InjectPoseA(poseA);
            InjectPoseB(poseB);
            InjectJoints(configs);
        }

        public void InjectPoseA(IBodyPose poseA)
        {
            _poseA = poseA as UnityEngine.Object;
            PoseA = poseA;
        }

        public void InjectPoseB(IBodyPose poseB)
        {
            _poseB = poseB as UnityEngine.Object;
            PoseB = poseB;
        }

        public void InjectJoints(IEnumerable<JointComparerConfig> configs)
        {
            _configs = new List<JointComparerConfig>(configs);
        }

        public void InjectOptionalTimeProvider(Func<float> timeProvider)
        {
            _timeProvider = timeProvider;
        }
        #endregion
    }
}
