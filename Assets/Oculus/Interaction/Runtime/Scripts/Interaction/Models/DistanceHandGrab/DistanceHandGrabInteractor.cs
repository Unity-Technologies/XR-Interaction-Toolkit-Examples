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

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The DistanceHandGrabInteractor allows grabbing DistanceHandGrabInteractables at a distance.
    /// It operates with HandGrabPoses to specify the final pose of the hand and manipulate the objects
    /// via IMovements in order to attract them, use them at a distance, etc.
    /// The DistanceHandGrabInteractor uses a IDistantCandidateComputer to detect far-away objects.
    /// </summary>
    public class DistanceHandGrabInteractor :
        PointerInteractor<DistanceHandGrabInteractor, DistanceHandGrabInteractable>,
        IHandGrabInteractor, IDistanceInteractor
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [Header("Grabbing")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;

        [SerializeField]
        private Transform _grabOrigin;

        [SerializeField, Optional]
        private Transform _gripPoint;

        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private UnityEngine.Object _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        [SerializeField]
        private DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> _distantCandidateComputer
            = new DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable>();

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabResult _cachedResult = new HandGrabResult();

        #region IHandGrabInteractor
        public IMovement Movement { get; set; }
        public bool MovementFinished { get; set; }

        public HandGrabTarget HandGrabTarget { get; } = new HandGrabTarget();

        public Transform WristPoint => _grabOrigin;
        public Transform PinchPoint => _pinchPoint;
        public Transform PalmPoint => _gripPoint;

        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabInteractable TargetInteractable => Interactable;
        #endregion

        public Pose Origin => _distantCandidateComputer.Origin;
        public Vector3 HitPoint { get; private set; }
        public IRelativeToRef DistanceInteractable => this.Interactable;

        #region IHandGrabState
        public virtual bool IsGrabbing => HasSelectedInteractable
            && (Movement != null && Movement.Stopped);
        public float FingersStrength { get; private set; }
        public float WristStrength { get; private set; }
        public Pose WristToGrabPoseOffset { get; private set; }

        public HandFingerFlags GrabbingFingers()
        {
            return this.GrabbingFingers(SelectedInteractable);
        }
        #endregion

        #region editor events
        protected virtual void Reset()
        {
            _hand = this.GetComponentInParent<IHand>() as MonoBehaviour;
            _handGrabApi = this.GetComponentInParent<HandGrabAPI>();
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            VelocityCalculator = _velocityCalculator as IVelocityCalculator;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(_distantCandidateComputer, nameof(_distantCandidateComputer));
            if (_velocityCalculator != null)
            {
                this.AssertField(VelocityCalculator, nameof(VelocityCalculator));
            }

            this.EndStart(ref _started);
        }

        #region life cycle

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            _handGrabShouldSelect = false;

            if (Interactable == null)
            {
                return;
            }

            UpdateTarget(Interactable);
            if (this.ComputeShouldSelect(Interactable, out _))
            {
                _handGrabShouldSelect = true;
            }
        }

        protected override void InteractableSet(DistanceHandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            UpdateTarget(Interactable);
        }

        protected override void InteractableUnset(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            SetGrabStrength(0f);
        }

        protected override void DoSelectUpdate()
        {
            _handGrabShouldUnselect = false;
            if (SelectedInteractable == null)
            {
                _handGrabShouldUnselect = true;
                return;
            }

            Pose handGrabPose = this.GetHandGrabPose();
            Movement.UpdateTarget(handGrabPose);
            Movement.Tick();

            if (this.ComputeShouldUnselect(SelectedInteractable))
            {
                _handGrabShouldUnselect = true;
            }
        }

        protected override void InteractableSelected(DistanceHandGrabInteractable interactable)
        {
            if (interactable != null)
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                this.Movement = this.GenerateMovement(interactable);
                SetGrabStrength(1f);
            }

            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnselected(interactable);
            this.Movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        protected override void HandlePointerEventRaised(PointerEvent evt)
        {
            base.HandlePointerEventRaised(evt);

            if (SelectedInteractable == null
                || !SelectedInteractable.ResetGrabOnGrabsUpdated)
            {
                return;
            }

            if (evt.Identifier != Identifier &&
                (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect))
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                TrySetTarget(SelectedInteractable, this.CurrentGrabType());
                this.Movement = this.GenerateMovement(SelectedInteractable);

                Pose fromPose = this.GetTargetGrabPose();
                PointerEvent pe = new PointerEvent(Identifier, PointerEventType.Move, fromPose, Data);
                SelectedInteractable.PointableElement.ProcessPointerEvent(pe);
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (Movement != null)
            {
                return Movement.Pose;
            }
            return this.GetHandGrabPose();
        }

        #endregion

        protected override bool ComputeShouldSelect()
        {
            return _handGrabShouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _handGrabShouldUnselect;
        }

        public override bool CanSelect(DistanceHandGrabInteractable interactable)
        {
            if (!base.CanSelect(interactable))
            {
                return false;
            }
            return this.CanInteractWith(interactable);
        }

        protected override DistanceHandGrabInteractable ComputeCandidate()
        {
            DistanceHandGrabInteractable interactable = _distantCandidateComputer.ComputeCandidate(
               DistanceHandGrabInteractable.Registry, this, out Vector3 bestHitPoint);
            HitPoint = bestHitPoint;

            if (interactable == null)
            {
                return null;
            }

            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable);
            GrabPoseScore score = this.GetPoseScore(interactable, selectingGrabTypes, ref _cachedResult);

            if (score.IsValid())
            {
                return interactable;
            }

            return null;
        }

        private GrabTypeFlags SelectingGrabTypes(IHandGrabInteractable interactable)
        {
            GrabTypeFlags selectingGrabTypes;
            if (State == InteractorState.Select
                || !this.ComputeShouldSelect(interactable, out selectingGrabTypes))
            {
                HandGrabInteraction.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
            }

            if (selectingGrabTypes == GrabTypeFlags.None)
            {
                selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
            }

            return selectingGrabTypes;
        }

        private void UpdateTarget(IHandGrabInteractable interactable)
        {
            WristToGrabPoseOffset = this.GetGrabOffset();
            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable);
            TrySetTarget(interactable, selectingGrabTypes);
            float grabStrength = HandGrabInteraction.ComputeHandGrabScore(this, interactable, out _);
            SetGrabStrength(grabStrength);
        }

        private bool TrySetTarget(IHandGrabInteractable interactable, GrabTypeFlags selectingGrabTypes)
        {
            if (this.TryCalculateBestGrab(interactable, selectingGrabTypes, out HandGrabTarget.GrabAnchor anchorMode, ref _cachedResult))
            {
                HandGrabTarget.Set(interactable.RelativeTo, interactable.HandAlignment, anchorMode, _cachedResult);
                return true;
            }
            return false;
        }

        private void SetGrabStrength(float strength)
        {
            FingersStrength = strength;
            WristStrength = strength;
        }

        #region Inject
        public void InjectAllDistanceHandGrabInteractor(HandGrabAPI handGrabApi,
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer,
            Transform grabOrigin,
            IHand hand, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectDistantCandidateComputer(distantCandidateComputer);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabApi)
        {
            _handGrabApi = handGrabApi;
        }

        public void InjectDistantCandidateComputer(
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer)
        {
            _distantCandidateComputer = distantCandidateComputer;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectGrabOrigin(Transform grabOrigin)
        {
            _grabOrigin = grabOrigin;
        }

        public void InjectOptionalGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as UnityEngine.Object;
            VelocityCalculator = velocityCalculator;
        }
        #endregion
    }
}
