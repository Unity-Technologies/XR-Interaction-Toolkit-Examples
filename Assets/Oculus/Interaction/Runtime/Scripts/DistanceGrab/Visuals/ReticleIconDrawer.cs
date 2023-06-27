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
using UnityEngine.Assertions;

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleIconDrawer : InteractorReticle<ReticleDataIcon>
    {
        [SerializeField, Interface(typeof(IDistanceInteractor))]
        private UnityEngine.Object _distanceInteractor;
        private IDistanceInteractor DistanceInteractor { get; set; }

        [SerializeField]
        private MeshRenderer _renderer;

        [SerializeField]
        private Transform _centerEye;

        [SerializeField]
        private Texture _defaultIcon;
        public Texture DefaultIcon
        {
            get
            {
                return _defaultIcon;
            }
            set
            {
                _defaultIcon = value;
            }
        }

        [SerializeField]
        private bool _constantScreenSize;
        public bool ConstantScreenSize
        {
            get
            {
                return _constantScreenSize;
            }
            set
            {
                _constantScreenSize = value;
            }
        }

        private Vector3 _originalScale;

        protected override IInteractorView Interactor { get; set; }
        protected override Component InteractableComponent => DistanceInteractor.DistanceInteractable as Component;

        #region Editor events
        protected virtual void OnValidate()
        {
            if (_renderer != null)
            {
                _renderer.sharedMaterial.mainTexture = _defaultIcon;
            }
        }
        #endregion

        protected virtual void Awake()
        {
            DistanceInteractor = _distanceInteractor as IDistanceInteractor;
            Interactor = DistanceInteractor;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_renderer, nameof(_renderer));
            this.AssertField(_centerEye, nameof(_centerEye));
            _originalScale = this.transform.localScale;
            this.EndStart(ref _started);
        }

        protected override void Draw(ReticleDataIcon dataIcon)
        {
            if (dataIcon != null
                && dataIcon.CustomIcon != null)
            {
                _renderer.material.mainTexture = dataIcon.CustomIcon;
            }
            else
            {
                _renderer.material.mainTexture = _defaultIcon;
            }

            if (!_constantScreenSize)
            {
                _renderer.transform.localScale = _originalScale * dataIcon.GetTargetSize().magnitude;
            }
            _renderer.enabled = true;
        }

        protected override void Align(ReticleDataIcon data)
        {
            this.transform.position = data.ProcessHitPoint(DistanceInteractor.HitPoint);

            if (_renderer.enabled)
            {
                Vector3 dirToTarget = (_centerEye.position - transform.position).normalized;
                transform.LookAt(transform.position - dirToTarget, Vector3.up);

                if (_constantScreenSize)
                {
                    float distance = Vector3.Distance(transform.position, _centerEye.position);
                    _renderer.transform.localScale = _originalScale * distance;
                }
            }
        }

        protected override void Hide()
        {
            _renderer.enabled = false;
        }

        #region Inject
        public void InjectAllReticleIconDrawer(IDistanceInteractor distanceInteractor,
            Transform centerEye, MeshRenderer renderer)
        {
            InjectDistanceInteractor(distanceInteractor);
            InjectCenterEye(centerEye);
            InjectRenderer(renderer);
        }

        public void InjectDistanceInteractor(IDistanceInteractor distanceInteractor)
        {
            _distanceInteractor = distanceInteractor as UnityEngine.Object;
            DistanceInteractor = distanceInteractor;
            Interactor = distanceInteractor;
        }

        public void InjectCenterEye(Transform centerEye)
        {
            _centerEye = centerEye;
        }

        public void InjectRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }
        #endregion
    }
}
