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
using UnityEngine.Events;

namespace Oculus.Interaction.Locomotion
{
    public class LocomotionTurnerInteractorEventsWrapper : MonoBehaviour
    {
        [SerializeField]
        private LocomotionTurnerInteractor _turner;

        [SerializeField]
        private UnityEvent _whenTurnDirectionLeft;

        [SerializeField]
        private UnityEvent _whenTurnDirectionRight;

        public UnityEvent WhenTurnDirectionLeft => _whenTurnDirectionLeft;
        public UnityEvent WhenTurnDirectionRight => _whenTurnDirectionRight;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_turner, nameof(_turner));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _turner.WhenTurnDirectionChanged += HandleTurnDirectionChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _turner.WhenTurnDirectionChanged -= HandleTurnDirectionChanged;
            }
        }

        private void HandleTurnDirectionChanged(float dir)
        {
            if (dir > 0)
            {
                _whenTurnDirectionLeft.Invoke();
            }
            else if (dir < 0)
            {
                _whenTurnDirectionRight.Invoke();
            }
        }

        #region Inject

        public void InjectAllLocomotionTurnerInteractorEventsWrapper(LocomotionTurnerInteractor turner)
        {
            InjectTurner(turner);
        }

        public void InjectTurner(LocomotionTurnerInteractor turner)
        {
            _turner = turner;
        }

        #endregion

    }
}
