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
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The HandGrabInteractor allows grabbing objects while having the hands snap to them
    /// adopting a previously authored HandPose.
    /// There are different snapping techniques available, and when None is selected it will
    /// behave as a normal GrabInteractor.
    /// </summary>
    public class HandGrabInteractor : PointerInteractor<HandGrabInteractor, HandGrabInteractable>,
        IHandGrabInteractor, IRigidbodyRef
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;

        [SerializeField]
        private bool _hoverOnZeroStrength = false;
        public bool HoverOnZeroStrength
        {
            get
            {
                return _hoverOnZeroStrength;
            }
            set
            {
                _hoverOnZeroStrength = value;
            }
        }

        [SerializeField]
        private Transform _grabOrigin;

        [SerializeField, Optional]
        private Transform _gripPoint;
        [SerializeField, Optional]
        private SphereCollider _gripCollider;
        [SerializeField, Optional]
        private Transform _pinchPoint;
        [SerializeField, Optional]
        private SphereCollider _pinchCollider;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private UnityEngine.Object _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabResult _cachedResult = new HandGrabResult();
        private HandGrabInteractable _selectedInteractableOverride;

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

        #region IRigidbodyRef
        public Rigidbody Rigidbody => _rigidbody;
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
            _nativeId = 0x4847726162497472;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            Collider[] colliders = Rigidbody.GetComponentsInChildren<Collider>();

            this.AssertCollectionField(colliders, nameof(colliders),
                $"The associated {AssertUtils.Nicify(nameof(Rigidbody))} must have at least one Collider.");

            foreach (Collider collider in colliders)
            {
                this.AssertIsTrue(collider.isTrigger,
                    $"Associated Colliders in the {AssertUtils.Nicify(nameof(Rigidbody))} must be marked as Triggers.");
            }
            if (_pinchPoint != null)
            {
                this.AssertField(_pinchCollider, nameof(_pinchCollider),
                    whyItFailed: "When using Pinch Point a Pinch Collider is required.");
            }
            if (_gripPoint != null)
            {
                this.AssertField(_gripCollider, nameof(_gripCollider),
                    whyItFailed: "When using Grip Point a Grip Collider is required.");
            }

            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(Hand, nameof(Hand));
            if (_velocityCalculator != null)
            {
                this.AssertField(VelocityCalculator, nameof(VelocityCalculator));
            }

            this.EndStart(ref _started);
        }

        #region life cycle

        /// <summary>
        /// Each call while the interactor is hovering, it checks whether there is an interaction
        /// being hovered and sets the target snap pose to it. In the HandToObject snapping
        /// behaviors this is relevant as the hand can approach the object progressively even before
        /// a true grab starts.
        /// </summary>
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

        protected override void InteractableSet(HandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            UpdateTarget(Interactable);
        }

        protected override void InteractableUnset(HandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            SetGrabStrength(0f);
        }

        /// <summary>
        /// Each call while the hand is selecting/grabbing an interactable, it moves the item to the
        /// new position while also attracting it towards the hand if the snapping mode requires it.
        ///
        /// In some cases the parameter can be null, for example if the selection was interrupted
        /// by another hand grabbing the object. In those cases it will come out of the release
        /// state once the grabbing gesture properly finishes.
        /// </summary>
        /// <param name="interactable">The selected item</param>
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

        /// <summary>
        /// When a new interactable is selected, start the grab at the ideal point. When snapping is
        /// involved that can be a point in the interactable offset from the hand
        /// which will be stored to progressively reduced it in the next updates,
        /// effectively attracting the object towards the hand.
        /// When no snapping is involved the point will be the grip point of the hand directly.
        /// Note: ideally this code would be in InteractableSelected but it needs
        /// to be called before the object is marked as active.
        /// </summary>
        /// <param name="interactable">The selected interactable</param>
        protected override void InteractableSelected(HandGrabInteractable interactable)
        {
            if (interactable != null)
            {
                WristToGrabPoseOffset = this.GetGrabOffset();
                this.Movement = this.GenerateMovement(interactable);
                SetGrabStrength(1f);
            }

            base.InteractableSelected(interactable);
        }

        /// <summary>
        /// When releasing an active interactable, calculate the releasing point in similar
        /// fashion to  InteractableSelected
        /// </summary>
        /// <param name="interactable">The released interactable</param>
        protected override void InteractableUnselected(HandGrabInteractable interactable)
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
            return _handGrabShouldUnselect
                || (_selectedInteractableOverride != null && _selectedInteractableOverride != SelectedInteractable);
        }

        public override bool CanSelect(HandGrabInteractable interactable)
        {
            if (!base.CanSelect(interactable))
            {
                return false;
            }
            return this.CanInteractWith(interactable);
        }

        /// <summary>
        /// Compute the best interactable to snap to. In order to do it the method measures
        /// the score from the current grip pose to the closes pose in the surfaces
        /// of each one of the interactables in the registry.
        /// Even though it returns the best interactable, it also saves the entire Snap pose to
        /// it in which the exact pose within the surface is already recorded to avoid recalculations
        /// within the same frame.
        /// </summary>
        /// <returns>The best interactable to snap the hand to.</returns>
        protected override HandGrabInteractable ComputeCandidate()
        {
            var interactables = HandGrabInteractable.Registry.List(this);
            float bestFingerScore = float.NegativeInfinity;
            GrabPoseScore bestPoseScore = GrabPoseScore.Max;
            HandGrabInteractable bestInteractable = null;

            foreach (HandGrabInteractable interactable in interactables)
            {
                GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable, bestFingerScore,
                    out float fingerScore);

                if (selectingGrabTypes == GrabTypeFlags.None)
                {
                    continue;
                }

                GrabPoseScore poseScore = this.GetPoseScore(interactable, selectingGrabTypes, ref _cachedResult);
                if (fingerScore > bestFingerScore
                    || poseScore.IsBetterThan(bestPoseScore))
                {
                    bestFingerScore = fingerScore;
                    bestPoseScore = poseScore;
                    bestInteractable = interactable;
                }
            }

            return bestInteractable;
        }

        private GrabTypeFlags SelectingGrabTypes(HandGrabInteractable interactable,
            float minFingerScoreRequired, out float fingerScore)
        {
            fingerScore = 1.0f;
            GrabTypeFlags selectingGrabTypes;
            if (State == InteractorState.Select
                || !this.ComputeShouldSelect(interactable, out selectingGrabTypes))
            {
                fingerScore = HandGrabInteraction.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
            }

            if (fingerScore < minFingerScoreRequired)
            {
                return GrabTypeFlags.None;
            }

            if (selectingGrabTypes == GrabTypeFlags.None)
            {
                if (_hoverOnZeroStrength)
                {
                    selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
                }
                else
                {
                    return GrabTypeFlags.None;
                }
            }

            if (_gripCollider != null
                && (selectingGrabTypes & GrabTypeFlags.Palm) != 0
                && !OverlapsSphere(interactable, _gripCollider))
            {
                selectingGrabTypes &= ~GrabTypeFlags.Palm;
            }

            if (_pinchCollider != null
                && (selectingGrabTypes & GrabTypeFlags.Pinch) != 0
                && !OverlapsSphere(interactable, _pinchCollider))
            {
                selectingGrabTypes &= ~GrabTypeFlags.Pinch;
            }

            return selectingGrabTypes;
        }

        /// <summary>
        /// Enforces selection on the passed interactable. Grabbing it in the next interaction iteration.
        /// </summary>
        /// <param name="interactable">The interactable to grab</param>
        /// <param name="allowManualRelease">If false, the interactable can only be released by calling ForceRelease</param>
        public void ForceSelect(HandGrabInteractable interactable, bool allowManualRelease = false)
        {
            _selectedInteractableOverride = interactable;
            SetComputeCandidateOverride(() => interactable);
            SetComputeShouldSelectOverride(() => ReferenceEquals(interactable, Interactable));
            if (!allowManualRelease)
            {
                SetComputeShouldUnselectOverride(() => !ReferenceEquals(interactable, SelectedInteractable), false);
            }
        }
        /// <summary>
        /// Enforces deselection in the currently grabbed interactable (if any)
        /// </summary>
        public void ForceRelease()
        {
            _selectedInteractableOverride = null;
            ClearComputeCandidateOverride();
            ClearComputeShouldSelectOverride();

            if (State == InteractorState.Select)
            {
                SetComputeShouldUnselectOverride(() => true);
            }
            else
            {
                ClearComputeShouldUnselectOverride();
            }
        }

        public override void SetComputeCandidateOverride(Func<HandGrabInteractable> computeCandidate, bool shouldClearOverrideOnSelect = true)
        {
            base.SetComputeCandidateOverride(() =>
            {
                HandGrabInteractable interactable = computeCandidate.Invoke();
                return interactable;
            },
            shouldClearOverrideOnSelect);
        }

        public override void Unselect()
        {
            if (State == InteractorState.Select
                && _selectedInteractableOverride != null
                && (SelectedInteractable == _selectedInteractableOverride
                    || SelectedInteractable == null))
            {
                _selectedInteractableOverride = null;
                ClearComputeShouldUnselectOverride();
            }
            base.Unselect();
        }

        private bool OverlapsSphere(HandGrabInteractable interactable, SphereCollider sphere)
        {
            Vector3 point = sphere.transform.position;
            float radius = sphere.bounds.extents.x;

            foreach (Collider collider in interactable.Colliders)
            {
                if (collider.enabled &&
                    Collisions.IsSphereWithinCollider(point, radius, collider))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateTarget(HandGrabInteractable interactable)
        {
            WristToGrabPoseOffset = this.GetGrabOffset();
            GrabTypeFlags selectingGrabTypes = SelectingGrabTypes(interactable, float.NegativeInfinity, out float grabStrength);
            TrySetTarget(interactable, selectingGrabTypes);
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

        public void InjectAllHandGrabInteractor(HandGrabAPI handGrabApi,
            Transform grabOrigin,
            IHand hand, Rigidbody rigidbody, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabAPI)
        {
            _handGrabApi = handGrabAPI;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
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

        public void InjectOptionalGripCollider(SphereCollider gripCollider)
        {
            _gripCollider = gripCollider;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalPinchCollider(SphereCollider pinchCollider)
        {
            _pinchCollider = pinchCollider;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as UnityEngine.Object;
            VelocityCalculator = velocityCalculator;
        }

        #endregion
    }
}
