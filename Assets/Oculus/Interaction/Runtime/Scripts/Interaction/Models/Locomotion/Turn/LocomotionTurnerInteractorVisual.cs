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
    public class LocomotionTurnerInteractorVisual : MonoBehaviour
    {
        [SerializeField]
        private LocomotionTurnerInteractor _turner;

        [SerializeField]
        private Transform _ring;
        [SerializeField]
        private Transform _pointer;

        [SerializeField, Optional]
        private Renderer _ringRenderer;
        [SerializeField, Optional]
        private Renderer _pointerRenderer;

        private static readonly Quaternion RING_ROTATION = Quaternion.Euler(0f, 90f, 180f);

        protected bool _started;

        protected virtual void Awake()
        {
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_turner, nameof(_turner));
            this.AssertField(_ring, nameof(_ring));
            this.AssertField(_pointer, nameof(_pointer));

            if (_ringRenderer == null)
            {
                _ring.TryGetComponent(out _ringRenderer);
            }

            if (_pointerRenderer == null)
            {
                _pointer.TryGetComponent(out _pointerRenderer);
            }
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _turner.WhenStateChanged += HandleTurnerStateChanged;
                _turner.WhenPreprocessed += HandleTurnerPostprocessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _turner.WhenStateChanged -= HandleTurnerStateChanged;
                _turner.WhenPreprocessed -= HandleTurnerPostprocessed;
            }
        }

        private void HandleTurnerStateChanged(InteractorStateChangeArgs stateArgs)
        {
            if (stateArgs.NewState == InteractorState.Disabled)
            {
                _ringRenderer.enabled = false;
                _pointerRenderer.enabled = false;
            }
            else
            {
                _ringRenderer.enabled = true;
                _pointerRenderer.enabled = true;

            }
        }

        private void HandleTurnerPostprocessed()
        {
            UpdatePoses();
        }

        private void UpdatePoses()
        {
            Pose origin = _turner.MidPoint;
            float offset = _turner.Value();

            _ring.SetPositionAndRotation(
                origin.position,
                origin.rotation * RING_ROTATION);

            _pointer.SetPositionAndRotation(
                _turner.Origin.position,
                Quaternion.LookRotation(Mathf.Sign(offset) * origin.right, origin.up));
        }

        #region Inject
        public void InjectAllLocomotionTurnerInteractorVisual(LocomotionTurnerInteractor turner, Transform ring, Transform pointer)
        {
            InjectTurner(turner);
            InjectRing(ring);
            InjectPointer(pointer);
        }

        public void InjectTurner(LocomotionTurnerInteractor turner)
        {
            _turner = turner;
        }

        public void InjectRing(Transform leftRing)
        {
            _ring = leftRing;
        }

        public void InjectPointer(Transform pointer)
        {
            _pointer = pointer;
        }

        public void InjectOptionalRingRenderer(Renderer ringRenderer)
        {
            _ringRenderer = ringRenderer;
        }

        public void InjectOptionalPointerRenderer(Renderer pointerRenderer)
        {
            _pointerRenderer = pointerRenderer;
        }

        #endregion
    }
}
