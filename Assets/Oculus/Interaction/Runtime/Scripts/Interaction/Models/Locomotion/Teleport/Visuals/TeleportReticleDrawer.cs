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
using Oculus.Interaction.Locomotion;
using UnityEngine;

namespace Oculus.Interaction.DistanceReticles
{
    public class TeleportReticleDrawer : InteractorReticle<ReticleDataTeleport>
    {
        [SerializeField]
        private TeleportInteractor _interactor;

        [SerializeField, Optional]
        private Renderer _validTargetRenderer;
        [SerializeField, Optional]
        private Renderer _invalidTargetRenderer;

        [SerializeField, Optional, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _progress;
        private IAxis1D Progress;

        [SerializeField, Optional, Interface(typeof(IActiveState))]
        private UnityEngine.Object _highlightState;
        private IActiveState HighlightState;

        protected override IInteractorView Interactor { get; set; }

        protected override Component InteractableComponent => _interactor.Interactable;

        private static readonly int _progressKey = Shader.PropertyToID("_Progress");
        private static readonly int _highlightKey = Shader.PropertyToID("_Highlight");

        protected virtual void Awake()
        {
            Progress = _progress as IAxis1D;
            HighlightState = _highlightState as IActiveState;
            Interactor = _interactor;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(_interactor, nameof(_interactor));

            _validTargetRenderer.enabled = false;
            _invalidTargetRenderer.enabled = false;

            this.EndStart(ref _started);
        }

        protected override void Align(ReticleDataTeleport data)
        {
            bool highlight = HighlightState != null && HighlightState.Active;
            data.Highlight(highlight);

            if (data.ReticleMode == ReticleDataTeleport.TeleportReticleMode.Hidden)
            {
                return;
            }
            Vector3 position = data.ProcessHitPoint(_interactor.ArcEnd.Point);
            Quaternion rotation = Quaternion.LookRotation(_interactor.ArcEnd.Normal);
            this.transform.SetPositionAndRotation(position, rotation);

            float progress = Progress != null ? Progress.Value() : 0f;
            Renderer reticle = null;
            if (data.ReticleMode == ReticleDataTeleport.TeleportReticleMode.ValidTarget)
            {
                reticle = _validTargetRenderer;
            }
            else if (data.ReticleMode == ReticleDataTeleport.TeleportReticleMode.InvalidTarget)
            {
                reticle = _invalidTargetRenderer;
            }

            if (reticle == null)
            {
                return;
            }

            UpdateReticle(data.ReticleMode);
            SetReticleProgress(reticle, progress);
            if (HighlightState != null)
            {
                SetReticleHighlight(reticle, highlight);
            }
        }

        protected override void Draw(ReticleDataTeleport data)
        {
            UpdateReticle(data.ReticleMode);
        }

        protected override void Hide()
        {
            if (_validTargetRenderer != null)
            {
                _validTargetRenderer.enabled = false;
            }
            if (_invalidTargetRenderer != null)
            {
                _invalidTargetRenderer.enabled = false;
            }
            if (_targetData != null)
            {
                _targetData.Highlight(false);
            }
        }

        private void SetReticleProgress(Renderer reticle, float progress)
        {
            reticle.material.SetFloat(_progressKey, progress);
        }

        private void SetReticleHighlight(Renderer reticle, bool highlight)
        {
            reticle.material.SetFloat(_highlightKey, highlight ? 1f : 0f);
        }

        private void UpdateReticle(ReticleDataTeleport.TeleportReticleMode reticleMode)
        {
            if (_validTargetRenderer != null)
            {
                _validTargetRenderer.enabled = reticleMode == ReticleDataTeleport.TeleportReticleMode.ValidTarget;
            }

            if (_invalidTargetRenderer != null)
            {
                _invalidTargetRenderer.enabled = reticleMode == ReticleDataTeleport.TeleportReticleMode.InvalidTarget;
            }
        }


        #region Inject

        public void InjectAllTeleportReticleDrawer(TeleportInteractor interactor)
        {
            InjectInteractor(interactor);
        }

        public void InjectInteractor(TeleportInteractor interactor)
        {
            _interactor = interactor;
        }

        public void InjectOptionalValidTargetRenderer(Renderer validTargetRenderer)
        {
            _validTargetRenderer = validTargetRenderer;
        }
        public void InjectOptionalInalidTargetRenderer(Renderer invalidTargetRenderer)
        {
            _invalidTargetRenderer = invalidTargetRenderer;
        }

        public void InjectOptionalProgress(IAxis1D progress)
        {
            _progress = progress as UnityEngine.Object;
            Progress = progress;
        }
        #endregion
    }
}
