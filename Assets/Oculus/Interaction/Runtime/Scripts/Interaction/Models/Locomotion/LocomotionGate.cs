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

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This Gate reads the Hand orientation towards the shoulder and decides
    /// if it should be in Teleport mode (hand horizontal) or Turning mode (hand vertical).
    /// It enables/disables said modes based on some Input ActiveStates (EnableShape and DisableShape).
    /// It outputs it result into two ActiveStates (for Teleport and Turn)
    /// </summary>
    public class LocomotionGate : MonoBehaviour
    {
        /// <summary>
        /// Hand that will be performing the Turn and Teleport
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        /// <summary>
        /// Shoulder of the relevant Hand, used for correctly
        /// measuring the angle of the wrist to swap between Turning or Teleport
        /// </summary>
        [SerializeField]
        private Transform _shoulder;

        [SerializeField]
        private bool _allowPalmDownGating = false;
        public bool AllowPalmDownGating
        {
            get
            {
                return _allowPalmDownGating;
            }
            set
            {
                _allowPalmDownGating = value;
            }
        }

        [SerializeField]
        private Vector2 _palmUpToTurnThresholds = new Vector2(50f, 95f);
        public Vector2 PalmUpToTurnThresholds => _palmUpToTurnThresholds;

        [SerializeField]
        private Vector2 _turnToPalmDownToThresholds = new Vector2(110f, 140f);
        public Vector2 TurnToPalmDownToThresholds => _turnToPalmDownToThresholds;

        /// <summary>
        /// When it becomes Active, if the hand is within the valid threshold, the
        /// gate will enter Teleport or Turning mode
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _enableShape;
        private IActiveState EnableShape { get; set; }

        /// <summary>
        /// When active, the gate will exit Teleport and Turning modes
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _disableShape;
        private IActiveState DisableShape { get; set; }

        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Turning mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _turningState;
        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Teleport mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _teleportState;

        protected bool _started;
        private bool _previousShapeEnabled;

        private LocomotionMode _activeMode = LocomotionMode.None;
        public LocomotionMode ActiveMode
        {
            get
            {
                return _activeMode;
            }
            private set
            {
                _activeMode = value;
                _teleportState.Active = _activeMode == LocomotionMode.TeleportUp
                    || _activeMode == LocomotionMode.TeleportDown;
                _turningState.Active = _activeMode == LocomotionMode.Turn;
            }
        }

        public enum LocomotionMode
        {
            None,
            TeleportUp,
            TeleportDown,
            Turn
        }

        public float WristLimit => _wristLimit;
        public float CurrentAngle { get; private set; }
        public Vector3 WristDirection { get; private set; }
        public Pose StabilizationPose { get; private set; } = Pose.identity;

        private const float _selectModeOnEnterThreshold = 0.5f;
        private const float _enterPoseThreshold = 0.5f;
        private const float _wristLimit = -70f;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            EnableShape = _enableShape as IActiveState;
            DisableShape = _disableShape as IActiveState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(EnableShape, nameof(EnableShape));
            this.AssertField(DisableShape, nameof(DisableShape));
            this.AssertField(_teleportState, nameof(_teleportState));
            this.AssertField(_turningState, nameof(_turningState));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandupdated;
                Disable();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandupdated;
                Disable();
            }
        }

        private void HandleHandupdated()
        {
            if (!Hand.GetRootPose(out Pose handPose))
            {
                Disable();
                return;
            }

            bool isRightHand = Hand.Handedness == Handedness.Right;
            Vector3 trackingUp = Vector3.up;
            Vector3 shoulderToHand = (handPose.position - _shoulder.position).normalized;
            Vector3 trackingRight = Vector3.Cross(trackingUp, shoulderToHand).normalized;
            trackingRight = isRightHand ? trackingRight : -trackingRight;
            Vector3 wristDir = isRightHand ? -handPose.forward : handPose.forward;
            Vector3 fingersDir = isRightHand ? -handPose.right : handPose.right;
            Vector3 palmDir = isRightHand ? -handPose.up : handPose.up;
            bool palmUp = (Vector3.Dot(palmDir, trackingUp) * 0.5 + 0.5f) > _enterPoseThreshold;
            bool flatHand = Mathf.Abs(Vector3.Dot(wristDir, trackingRight)) > _selectModeOnEnterThreshold;
            bool fingersAway = (Vector3.Dot(fingersDir, Vector3.ProjectOnPlane(shoulderToHand, trackingUp).normalized) * 0.5 + 0.5f) > _enterPoseThreshold;

            wristDir = Vector3.ProjectOnPlane(wristDir, shoulderToHand).normalized;
            float angle = Vector3.SignedAngle(wristDir, trackingRight, shoulderToHand);

            angle = Hand.Handedness == Handedness.Right ? -angle : angle;
            if (angle < _wristLimit)
            {
                angle += 360f;
            }

            CurrentAngle = angle;
            StabilizationPose = new Pose(_shoulder.position, Quaternion.LookRotation(shoulderToHand));
            WristDirection = wristDir;

            bool shapeGateEnabled = false;
            if (EnableShape.Active && !_previousShapeEnabled)
            {
                shapeGateEnabled = true;
            }
            _previousShapeEnabled = EnableShape.Active;

            if (ActiveMode == LocomotionMode.None
                && shapeGateEnabled
                && fingersAway)
            {
                if (flatHand)
                {
                    if (palmUp || _allowPalmDownGating)
                    {
                        ActiveMode = palmUp ? LocomotionMode.TeleportUp : LocomotionMode.TeleportDown;
                    }
                }
                else
                {
                    ActiveMode = LocomotionMode.Turn;
                }
                return;
            }

            if (ActiveMode != LocomotionMode.None
                && DisableShape.Active)
            {
                ActiveMode = LocomotionMode.None;
                return;
            }

            if (ActiveMode == LocomotionMode.TeleportUp)
            {
                if (CurrentAngle > _palmUpToTurnThresholds.y)
                {
                    ActiveMode = LocomotionMode.Turn;
                }
            }
            else if (ActiveMode == LocomotionMode.TeleportDown)
            {
                if (CurrentAngle < _turnToPalmDownToThresholds.x)
                {
                    ActiveMode = LocomotionMode.Turn;
                }
            }
            else if (ActiveMode == LocomotionMode.Turn)
            {
                if (CurrentAngle <= _palmUpToTurnThresholds.x)
                {
                    ActiveMode = LocomotionMode.TeleportUp;
                }
                else if (CurrentAngle >= _turnToPalmDownToThresholds.y)
                {
                    ActiveMode = LocomotionMode.TeleportDown;
                }
            }
        }

        private void Disable()
        {
            ActiveMode = LocomotionMode.None;
        }

        #region Inject

        public void InjectAllLocomotionGate(IHand hand, Transform shoulder,
            IActiveState enableShape, IActiveState disableShape,
            VirtualActiveState turningState, VirtualActiveState teleportState)
        {
            InjectHand(hand);
            InjectShoulder(shoulder);
            InjectEnableShape(enableShape);
            InjectDisableShape(disableShape);
            InjectTurningState(turningState);
            InjectTeleportState(teleportState);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        public void InjectEnableShape(IActiveState enableShape)
        {
            _enableShape = enableShape as UnityEngine.Object;
            EnableShape = enableShape;
        }

        public void InjectDisableShape(IActiveState disableShape)
        {
            _disableShape = disableShape as UnityEngine.Object;
            DisableShape = disableShape;
        }

        public void InjectTurningState(VirtualActiveState turningState)
        {
            _turningState = turningState;
        }

        public void InjectTeleportState(VirtualActiveState teleportState)
        {
            _teleportState = teleportState;
        }

        #endregion
    }
}
