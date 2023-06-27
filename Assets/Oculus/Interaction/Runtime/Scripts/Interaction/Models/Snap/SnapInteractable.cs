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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// SnapInteractables provide Pose targets for SnapInteractors to translate and rotate towards.
    /// </summary>
    public class SnapInteractable : Interactable<SnapInteractor, SnapInteractable>,
        IRigidbodyRef
    {
        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// By default will use the transform pose as the target pose.
        /// A SnapPoseDelegate can be provided to supply custom target pose logic.
        /// </summary>
        [FormerlySerializedAs("_snapPosesProvider")]
        [FormerlySerializedAs("_posesProvider")]
        [SerializeField, Optional, Interface(typeof(ISnapPoseDelegate))]
        private UnityEngine.Object _snapPoseDelegate;
        private ISnapPoseDelegate SnapPoseDelegate { get; set; }

        /// <summary>
        /// By default SnapInteractors will ease towards SnapInteractables.
        /// A MovementProvider can be provided to supply custom movement logic.
        /// </summary>
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private UnityEngine.Object _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        private static CollisionInteractionRegistry<SnapInteractor, SnapInteractable> _registry = null;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
            SnapPoseDelegate = _snapPoseDelegate as ISnapPoseDelegate;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Rigidbody, nameof(Rigidbody));
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<SnapInteractor, SnapInteractable>();
                SetRegistry(_registry);
            }
            if (MovementProvider == null)
            {
                MovementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                _movementProvider = MovementProvider as MonoBehaviour;
            }
            this.EndStart(ref _started);
        }

        protected override void InteractorAdded(SnapInteractor interactor)
        {
            base.InteractorAdded(interactor);
            if (SnapPoseDelegate != null)
            {
                SnapPoseDelegate.TrackElement(interactor.Identifier, interactor.SnapPose);
            }
        }

        protected override void InteractorRemoved(SnapInteractor interactor)
        {
            base.InteractorRemoved(interactor);
            if (SnapPoseDelegate != null)
            {
                SnapPoseDelegate.UntrackElement(interactor.Identifier);
            }
        }

        protected override void SelectingInteractorAdded(SnapInteractor interactor)
        {
            base.SelectingInteractorAdded(interactor);
            if (SnapPoseDelegate != null)
            {
                SnapPoseDelegate.SnapElement(interactor.Identifier, interactor.SnapPose);
            }
        }

        protected override void SelectingInteractorRemoved(SnapInteractor interactor)
        {
            base.SelectingInteractorRemoved(interactor);
            if (SnapPoseDelegate != null)
            {
                SnapPoseDelegate.UnsnapElement(interactor.Identifier);
            }
        }

        public void InteractorHoverUpdated(SnapInteractor interactor)
        {
            if (SnapPoseDelegate != null)
            {
                SnapPoseDelegate.MoveTrackedElement(interactor.Identifier, interactor.SnapPose);
            }
        }

        public bool PoseForInteractor(SnapInteractor interactor, out Pose result)
        {
            if (SnapPoseDelegate != null)
            {
                return SnapPoseDelegate.SnapPoseForElement(
                    interactor.Identifier,
                    interactor.SnapPose,
                    out result);
            }

            result = this.transform.GetPose();
            return true;
        }

        public IMovement GenerateMovement(in Pose from, SnapInteractor interactor)
        {
            if (PoseForInteractor(interactor, out Pose to))
            {
                IMovement movement = MovementProvider.CreateMovement();
                movement.StopAndSetPose(from);
                movement.MoveTo(to);
                return movement;
            }
            return null;
        }

        #region Inject
        public void InjectAllSnapInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as UnityEngine.Object;
            MovementProvider = provider;
        }

        public void InjectOptionalSnapPoseDelegate(ISnapPoseDelegate snapPoseDelegate)
        {
            _snapPoseDelegate = snapPoseDelegate as UnityEngine.Object;
            SnapPoseDelegate = snapPoseDelegate;
        }

        #endregion
    }
}
