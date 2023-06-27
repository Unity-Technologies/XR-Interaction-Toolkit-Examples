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
using System;
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// LocomotionTurnerInteractor is an interactor that processes the position of
    /// a point in space and outputs an Axis1D value based on the lateral displacement of said point.
    /// This interactor does not require any interactable.
    /// </summary>
    public class LocomotionTurnerInteractor : Interactor<LocomotionTurnerInteractor, LocomotionTurnerInteractable>
        , IAxis1D
    {
        [SerializeField]
        [Tooltip("Point in space used to drive the axis.")]
        private Transform _origin;

        [SerializeField, Interface(typeof(ISelector))]
        [Tooltip("Selector for the interactor.")]
        private UnityEngine.Object _selector;

        [SerializeField]
        [Tooltip("Point used to stabilize the rotation of the point")]
        private Transform _stabilizationPoint;

        [SerializeField, Interface(typeof(ITrackingToWorldTransformer))]
        [Tooltip("Transformer is required so calculations can be done in Tracking space")]
        private UnityEngine.Object _transformer;
        /// <summary>
        /// Transformer is required so calculations can be done in Tracking space
        /// </summary>
        public ITrackingToWorldTransformer Transformer;

        [SerializeField]
        [Tooltip("Offset from the center point at which the pointer will be dragged")]
        private float _dragThresold = 0.1f;
        /// <summary>
        /// Offset from the center point at which the pointer will be dragged
        /// </summary>
        public float DragThresold
        {
            get
            {
                return _dragThresold;
            }
            set
            {
                _dragThresold = value;
            }
        }

        private Pose _midPoint = Pose.identity;

        /// <summary>
        /// Center point where the Axis value is 0
        /// </summary>
        public Pose MidPoint => Transformer.ToWorldPose(_midPoint);
        /// <summary>
        /// Point of the actual origin in world space.
        /// The offset from Origin to MidPoint indicates the Axis value.
        /// </summary>
        public Pose Origin => _origin.GetPose();

        private float _axisValue = 0f;

        private Action<float> _whenTurnDirectionChanged = delegate { };

        /// <summary>
        /// Event broadcasted when the Axis changes sign
        /// </summary>
        public event Action<float> WhenTurnDirectionChanged
        {
            add
            {
                _whenTurnDirectionChanged += value;
            }
            remove
            {
                _whenTurnDirectionChanged -= value;
            }
        }


        public override bool ShouldHover => State == InteractorState.Normal;
        public override bool ShouldUnhover => false;

        protected override void Awake()
        {
            base.Awake();
            Transformer = _transformer as ITrackingToWorldTransformer;
            Selector = _selector as ISelector;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_origin, nameof(_origin));
            this.AssertField(_stabilizationPoint, nameof(_stabilizationPoint));
            this.AssertField(Transformer, nameof(Transformer));
            this.AssertField(Selector, nameof(Selector));
            this.EndStart(ref _started);
        }

        protected override void HandleEnabled()
        {
            base.HandleEnabled();

            Pose pointer = _origin.GetPose();
            InitializeMidPoint(pointer);
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();
            UpdatePointers();
        }

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();
            UpdatePointers();
        }

        private void UpdatePointers()
        {
            Pose pointer = _origin.GetPose();
            UpdateMidPoint(pointer, MidPoint);
            DragMidPoint(MidPoint);
            UpdateAxisValue(pointer, MidPoint);
        }

        private void InitializeMidPoint(Pose pointer)
        {
            Vector3 direction = Vector3.ProjectOnPlane(pointer.position - _stabilizationPoint.position, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 point = pointer.position;
            _midPoint = Transformer.ToTrackingPose(new Pose(point, rotation));
        }

        private void UpdateMidPoint(Pose pointer, Pose midPoint)
        {
            float length = Vector3.ProjectOnPlane(pointer.position - _stabilizationPoint.position, Vector3.up).magnitude;
            Vector3 position = _stabilizationPoint.position + midPoint.forward * length;
            position.y = pointer.position.y;
            Quaternion rotation = midPoint.rotation;
            _midPoint = Transformer.ToTrackingPose(new Pose(position, rotation));
        }

        private void DragMidPoint(Pose worldMidPoint)
        {
            Vector3 midPointPos = worldMidPoint.position;
            float distance = Mathf.Abs(_axisValue) - _dragThresold * this.transform.lossyScale.x;
            if (distance <= 0)
            {
                return;
            }

            Vector3 right = worldMidPoint.right;
            float direction = Math.Sign(_axisValue);
            midPointPos += right * direction * distance;

            Vector3 lookDirection = Vector3.ProjectOnPlane(midPointPos - _stabilizationPoint.position, Vector3.up).normalized;
            Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

            _midPoint = Transformer.ToTrackingPose(new Pose(midPointPos, rotation));
        }

        private void UpdateAxisValue(Pose pointer, Pose origin)
        {
            float prevSign = Mathf.Sign(_axisValue);
            Vector3 diff = pointer.position - origin.position;
            Vector3 deviation = Vector3.Project(pointer.position - origin.position, origin.right);
            _axisValue = deviation.magnitude * Mathf.Sign(Vector3.Dot(origin.right, diff));
            if (prevSign != Mathf.Sign(_axisValue))
            {
                _whenTurnDirectionChanged(prevSign);
            }
        }

        /// <summary>
        /// Axis value of the interactor, between -1 and 1 where
        /// a negative number indicates a  left turn and a positive
        /// value a right turn
        /// </summary>
        /// <returns>A value between -1 and 1</returns>
        public float Value()
        {
            return Mathf.Clamp(_axisValue / (_dragThresold * this.transform.lossyScale.x), -1f, 1f);
        }

        protected override LocomotionTurnerInteractable ComputeCandidate()
        {
            return null;
        }

        #region Inject

        public void InjectAllLocomotionTurnerInteractor(Transform origin,
            ISelector selector,
            Transform stabilizationPoint,
            ITrackingToWorldTransformer transformer)
        {
            InjectOrigin(origin);
            InjectSelector(selector);
            InjectStabilizationPoint(stabilizationPoint);
            InjectTransformer(transformer);
        }

        public void InjectOrigin(Transform origin)
        {
            _origin = origin;
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        public void InjectStabilizationPoint(Transform stabilizationPoint)
        {
            _stabilizationPoint = stabilizationPoint;
        }

        public void InjectTransformer(ITrackingToWorldTransformer transformer)
        {
            _transformer = transformer as UnityEngine.Object;
            Transformer = transformer;
        }
        #endregion
    }

    /// <summary>
    /// LocomotionTurnerInteractor does not require and Interactable.
    /// This class is left empty and in a differently named file
    /// so it cannot be used as a Component in the inspector.
    /// </summary>
    public class LocomotionTurnerInteractable : Interactable<LocomotionTurnerInteractor, LocomotionTurnerInteractable> { }
}
