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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// SnapInteractors can snap to poses provided by SnapInteractables.
    /// SnapInteractors select and unselect in response to a PointableElement.
    /// SnapInteractors hover when the PointableElement has selecting pointers and snap to the best
    /// SnapInteractable pose when the PointableElement has no more selecting pointers.
    /// </summary>
    public class SnapInteractor : Interactor<SnapInteractor, SnapInteractable>,
        IRigidbodyRef
    {
        [SerializeField]
        private PointableElement _pointableElement;
        public IPointableElement PointableElement => _pointableElement;

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField]
        private float _distanceThreshold = 0.01f;

        [SerializeField, Optional]
        [FormerlySerializedAs("_snapPoint")]
        [FormerlySerializedAs("_dropPoint")]
        private Transform _snapPoseTransform;
        public Pose SnapPose => _snapPoseTransform.GetPose();

        [SerializeField, Optional]
        private SnapInteractable _defaultInteractable = null;

        /// <summary>
        /// Interactable to automatically snap to
        /// when the associated Pointable is not being pointed at for Time-Out seconds.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Interactable to automatically snap to " +
            "when the associated Pointable is not being pointed at for Time-Out seconds")]
        private SnapInteractable _timeOutInteractable = null;

        /// <summary>
        /// When the associated Pointable is not being pointed at for Time-Out seconds
        /// the SnapInteractor will snap to the TimeOutInteractable, unless it is null.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("When the associated Pointable is not being pointed at for Time-Out seconds " +
            "the SnapInteractor will snap to the TimeOutInteractable, unless it is null.")]
        private float _timeOut = 0f;

        private float _idleStarted = -1f;
        private IMovement _movement = null;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
            _pointableElement = this.GetComponentInParent<PointableElement>();
        }
        #endregion

        #region Properties

        public float DistanceThreshold
        {
            get
            {
                return _distanceThreshold;
            }

            set
            {
                _distanceThreshold = value;
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_pointableElement, nameof(_pointableElement));
            this.AssertField(Rigidbody, nameof(Rigidbody));

            if (_snapPoseTransform == null)
            {
                _snapPoseTransform = this.transform;
            }

            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised += HandlePointerEventRaised;
                if (_defaultInteractable != null)
                {
                    SetComputeCandidateOverride(() => _defaultInteractable, true);
                    SetComputeShouldSelectOverride(()=>true, true);
                }
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
            base.OnDisable();
        }

        #endregion

        #region Interactor Lifecycle

        protected override bool ComputeShouldSelect()
        {
            return _shouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _shouldUnselect;
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            _shouldUnselect = false;

            if (Interactable == null)
            {
                return;
            }

            GeneratePointerEvent(PointerEventType.Move);
            Interactable.InteractorHoverUpdated(this);
        }

        private bool _shouldSelect = false;
        private bool _shouldUnselect = false;

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();

            if (_movement == null || Interactable == null)
            {
                _shouldUnselect = true;
                return;
            }

            if (Interactable.PoseForInteractor(this, out Pose targetPose))
            {
                _movement.UpdateTarget(targetPose);
                _movement.Tick();
                GeneratePointerEvent(PointerEventType.Move);
            }
            else
            {
                _shouldUnselect = true;
            }
        }

        protected override void InteractableSet(SnapInteractable interactable)
        {
            base.InteractableSet(interactable);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Hover);
            }
        }

        protected override void InteractableUnset(SnapInteractable interactable)
        {
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Unhover);
            }
            base.InteractableUnset(interactable);
        }

        protected override void InteractableSelected(SnapInteractable interactable)
        {
            base.InteractableSelected(interactable);
            _shouldSelect = false;
            if (interactable != null)
            {
                _movement = interactable.GenerateMovement(_snapPoseTransform.GetPose(), this);
                if (_movement != null)
                {
                    GeneratePointerEvent(PointerEventType.Select);
                }
            }
        }

        protected override void InteractableUnselected(SnapInteractable interactable)
        {
            _movement?.StopAndSetPose(_movement.Pose);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEventType.Unselect);
            }
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        #endregion

        #region Pointable

        protected virtual void HandlePointerEventRaised(PointerEvent evt)
        {
            if (_pointableElement.SelectingPointsCount == 0 &&
                evt.Identifier != Identifier &&
                evt.Type == PointerEventType.Unselect)
            {
                if (Interactable != null)
                {
                    _shouldSelect = true;
                }
            }

            if (evt.Identifier == Identifier &&
                evt.Type == PointerEventType.Cancel &&
                Interactable != null)
            {
                Interactable.RemoveInteractorByIdentifier(Identifier);
            }
        }

        private void GeneratePointerEvent(PointerEventType pointerEventType)
        {
            Pose pose = ComputePointerPose();
            _pointableElement.ProcessPointerEvent(
                new PointerEvent(
                    Identifier, pointerEventType, pose, Data));
        }

        protected override void DoPreprocess()
        {
            if (_pointableElement.Points.Count == 0)
            {
                if (_idleStarted < 0)
                {
                    _idleStarted = Time.time;
                }
            }
            else
            {
                _idleStarted = -1;
            }
        }

        protected Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }

            return SnapPose;
        }
        #endregion

        private bool TimedOut()
        {
            return _timeOutInteractable != null
                && _timeOut >= 0f
                && _idleStarted >= 0f
                && Time.time - _idleStarted > _timeOut;
        }

        protected override SnapInteractable ComputeCandidate()
        {
            if (TimedOut())
            {
                _shouldSelect = true;
                return _timeOutInteractable;
            }

            if (_pointableElement.SelectingPointsCount == 0)
            {
                if (!_shouldSelect)
                {
                    return null;
                }
                else
                {
                    return Interactable;
                }
            }

            float distanceThresholdSqr = _distanceThreshold * _distanceThreshold;

            SnapInteractable closestInteractable = null;
            float bestPositionDeltaSqr = float.MaxValue;
            float bestAngularDelta = float.MaxValue;

            var interactables = SnapInteractable.Registry.List(this);
            foreach (SnapInteractable interactable in interactables)
            {
                if (!interactable.PoseForInteractor(this, out Pose pose))
                {
                    continue;
                }

                float positionDeltaSqr = (pose.position - _snapPoseTransform.position).sqrMagnitude;
                if (positionDeltaSqr > bestPositionDeltaSqr)
                {
                    continue;
                }

                float angularDist = Quaternion.Angle(pose.rotation, _snapPoseTransform.rotation);
                if (Mathf.Abs(positionDeltaSqr - bestPositionDeltaSqr) < distanceThresholdSqr &&
                    angularDist >= bestAngularDelta)
                {
                    continue;
                }

                bestPositionDeltaSqr = positionDeltaSqr;
                bestAngularDelta = angularDist;
                closestInteractable = interactable;

            }

            return closestInteractable;
        }

        #region Inject

        public void InjectAllSnapInteractor(PointableElement pointableElement, Rigidbody rigidbody)
        {
            InjectPointableElement(pointableElement);
            InjectRigidbody(rigidbody);
        }

        public void InjectPointableElement(PointableElement pointableElement)
        {
            _pointableElement = pointableElement;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalSnapPoseTransform(Transform snapPoint)
        {
            _snapPoseTransform = snapPoint;
        }

        public void InjectOptionalTimeOutInteractable(SnapInteractable interactable)
        {
            _timeOutInteractable = interactable;
        }

        public void InjectOptionaTimeOut(float timeOut)
        {
            _timeOut = timeOut;
        }
        #endregion
    }
}
