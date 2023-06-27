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

namespace Oculus.Interaction
{
    public class PinchPointerVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IInteractor))]
        private UnityEngine.Object _interactor;
        private IInteractor Interactor;

        [SerializeField]
        private SkinnedMeshRenderer _skinnedMeshRenderer;

        [SerializeField]
        private Vector3 _localOffset = Vector3.zero;
        public Vector3 LocalOffset
        {
            get
            {
                return _localOffset;
            }
            set
            {
                _localOffset = value;
            }
        }

        [SerializeField]
        private AnimationCurve _remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve RemapCurve
        {
            get
            {
                return _remapCurve;
            }
            set
            {
                _remapCurve = value;
            }
        }

        [SerializeField]
        private Vector2 _alphaRange = new Vector2(.1f, .4f);
        public Vector2 AlphaRange
        {
            get
            {
                return _alphaRange;
            }
            set
            {
                _alphaRange = value;
            }
        }

        [SerializeField]
        private Color _tint = Color.white;
        public Color Tint
        {
            get
            {
                return _tint;
            }
            set
            {
                _tint = value;
            }
        }

        [SerializeField, Interface(typeof(IAxis1D)), Optional]
        private UnityEngine.Object _progress;
        private IAxis1D Progress;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Interactor = _interactor as IInteractor;
            Progress = _progress as IAxis1D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_skinnedMeshRenderer, nameof(_skinnedMeshRenderer));
            this.AssertField(_remapCurve, nameof(_remapCurve));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged += HandleStateChanged;
                Interactor.WhenPreprocessed += HandlePostprocessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged -= HandleStateChanged;
                Interactor.WhenPreprocessed -= HandlePostprocessed;
            }
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            position += rotation * _localOffset;
            this.transform.SetPositionAndRotation(position, rotation);
        }

        private void HandleStateChanged(InteractorStateChangeArgs stateArgs)
        {

            if (stateArgs.NewState == InteractorState.Disabled)
            {
                _skinnedMeshRenderer.enabled = false;
            }
            else
            {
                _skinnedMeshRenderer.enabled = true;
            }
        }

        private void HandlePostprocessed()
        {
            var mappedPinchStrength = _remapCurve.Evaluate(Progress.Value());
            _skinnedMeshRenderer.SetBlendShapeWeight(0, mappedPinchStrength * 100f);
            _skinnedMeshRenderer.SetBlendShapeWeight(1, mappedPinchStrength * 100f);
            UpdateColor(Interactor.State == InteractorState.Select, mappedPinchStrength);
        }

        private void UpdateColor(bool highlight, float mappedPinchStrength)
        {
            Color color = Tint;
            color.a *= highlight ? 1f : Mathf.Lerp(_alphaRange.x, _alphaRange.y, mappedPinchStrength);
            _skinnedMeshRenderer.material.color = color;
        }

        #region Inject

        public void InjectAllPinchPointerVisual(IInteractor interactor,
            SkinnedMeshRenderer skinnedMeshRenderer)
        {
            InjectInteractor(interactor);
            InjectSkinnedMeshRenderer(skinnedMeshRenderer);
        }

        public void InjectInteractor(IInteractor interactor)
        {
            Interactor = interactor;
            _interactor = interactor as UnityEngine.Object;
        }

        public void InjectSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
        }

        #endregion
    }
}
