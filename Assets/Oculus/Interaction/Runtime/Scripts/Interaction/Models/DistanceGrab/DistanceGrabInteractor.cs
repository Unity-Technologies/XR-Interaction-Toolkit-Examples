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

using Oculus.Interaction.Throw;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interactor allows grabbing objects at a distance and will move them using configurable IMovements.
    /// It uses a IDistantCandidateComputer in order to Hover the best candidate.
    /// </summary>
    public class DistanceGrabInteractor : PointerInteractor<DistanceGrabInteractor, DistanceGrabInteractable>,
        IDistanceInteractor
    {
        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _selector;

        [SerializeField, Optional]
        private Transform _grabCenter;

        [SerializeField, Optional]
        private Transform _grabTarget;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private UnityEngine.Object _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        [SerializeField]
        private DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> _distantCandidateComputer
            = new DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable>();

        private IMovement _movement;

        public Pose Origin => _distantCandidateComputer.Origin;

        public Vector3 HitPoint { get; private set; }

        public IRelativeToRef DistanceInteractable => this.Interactable;

        public float BestInteractableWeight { get; private set; } = float.MaxValue;

        protected override void Awake()
        {
            base.Awake();
            Selector = _selector as ISelector;
            VelocityCalculator = _velocityCalculator as IVelocityCalculator;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(_distantCandidateComputer, nameof(_distantCandidateComputer));

            if (_grabCenter == null)
            {
                _grabCenter = transform;
            }

            if (_grabTarget == null)
            {
                _grabTarget = _grabCenter;
            }

            if (_velocityCalculator != null)
            {
                this.AssertField(VelocityCalculator, nameof(VelocityCalculator));
            }
            this.EndStart(ref _started);
        }

        protected override void DoPreprocess()
        {
            transform.position = _grabCenter.position;
            transform.rotation = _grabCenter.rotation;
        }

        protected override DistanceGrabInteractable ComputeCandidate()
        {
            DistanceGrabInteractable bestCandidate = _distantCandidateComputer.ComputeCandidate(
                DistanceGrabInteractable.Registry, this, out Vector3 hitPoint);
            HitPoint = hitPoint;
            return bestCandidate;
        }

        protected override void InteractableSelected(DistanceGrabInteractable interactable)
        {
            _movement = interactable.GenerateMovement(_grabTarget.GetPose());
            base.InteractableSelected(interactable);
            interactable.WhenPointerEventRaised += HandleOtherPointerEventRaised;
        }

        protected override void InteractableUnselected(DistanceGrabInteractable interactable)
        {
            interactable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            _movement?.StopAndSetPose(_movement.Pose);
            base.InteractableUnselected(interactable);
            _movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        private void HandleOtherPointerEventRaised(PointerEvent evt)
        {
            if (SelectedInteractable == null)
            {
                return;
            }

            if (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect)
            {
                Pose toPose = _grabTarget.GetPose();
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    _movement = SelectedInteractable.GenerateMovement(toPose);
                    SelectedInteractable.PointableElement.ProcessPointerEvent(
                        new PointerEvent(Identifier, PointerEventType.Move, _movement.Pose, Data));
                }
            }

            if (evt.Identifier == Identifier && evt.Type == PointerEventType.Cancel)
            {
                SelectedInteractable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _movement.Pose;
            }
            return _grabTarget.GetPose();
        }

        protected override void DoSelectUpdate()
        {
            DistanceGrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                return;
            }

            _movement.UpdateTarget(_grabTarget.GetPose());
            _movement.Tick();
        }

        #region Inject
        public void InjectAllDistanceGrabInteractor(ISelector selector,
            DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> distantCandidateComputer)
        {
            InjectSelector(selector);
            InjectDistantCandidateComputer(distantCandidateComputer);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        public void InjectDistantCandidateComputer(DistantCandidateComputer<DistanceGrabInteractor, DistanceGrabInteractable> distantCandidateComputer)
        {
            _distantCandidateComputer = distantCandidateComputer;
        }

        public void InjectOptionalGrabCenter(Transform grabCenter)
        {
            _grabCenter = grabCenter;
        }

        public void InjectOptionalGrabTarget(Transform grabTarget)
        {
            _grabTarget = grabTarget;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as UnityEngine.Object;
            VelocityCalculator = velocityCalculator;
        }

        #endregion
    }
}
