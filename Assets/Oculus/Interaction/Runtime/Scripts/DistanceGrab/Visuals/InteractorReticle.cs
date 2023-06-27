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

namespace Oculus.Interaction.DistanceReticles
{
    public abstract class InteractorReticle<TReticleData> : MonoBehaviour
        where TReticleData : class, IReticleData
    {
        [SerializeField]
        private bool _visibleDuringSelect = false;
        private bool VisibleDuringSelect
        {
            get
            {
                return _visibleDuringSelect;
            }
            set
            {
                _visibleDuringSelect = value;
            }
        }

        protected bool _started;
        protected TReticleData _targetData;
        private bool _drawn;

        protected abstract IInteractorView Interactor { get; set; }
        protected abstract Component InteractableComponent { get; }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Interactor, nameof(Interactor));
            Hide();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged += HandleStateChanged;
                Interactor.WhenPostprocessed += HandlePostProcessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactor.WhenStateChanged -= HandleStateChanged;
                Interactor.WhenPostprocessed -= HandlePostProcessed;
            }
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            if (args.NewState == InteractorState.Normal
                   || args.NewState == InteractorState.Disabled)
            {
                InteractableUnset();
            }
            else if (args.NewState == InteractorState.Hover
                && args.PreviousState != InteractorState.Select)
            {
                InteractableSet(InteractableComponent);
            }
        }

        private void HandlePostProcessed()
        {
            if (_targetData != null
                  && (Interactor.State == InteractorState.Hover
                  || (Interactor.State == InteractorState.Select && _visibleDuringSelect)))
            {
                if (!_drawn)
                {
                    _drawn = true;
                    Draw(_targetData);
                }
                Align(_targetData);
            }
            else if (_drawn)
            {
                _drawn = false;
                Hide();
            }
        }

        private void InteractableSet(Component interactable)
        {
            if (interactable != null
                && interactable.TryGetComponent(out _targetData))
            {
                _drawn = false;
            }
            else
            {
                _targetData = null;
            }
        }

        private void InteractableUnset()
        {
            if (_drawn)
            {
                _drawn = false;
                Hide();
            }
            _targetData = default(TReticleData);
        }

        #region Drawing
        protected abstract void Draw(TReticleData data);
        protected abstract void Align(TReticleData data);
        protected abstract void Hide();
        #endregion
    }
}
