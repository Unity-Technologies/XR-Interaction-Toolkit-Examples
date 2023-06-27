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

namespace Oculus.Interaction
{
    public class ActiveStateGate : MonoBehaviour, IActiveState
    {
        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _openSelector;
        private ISelector OpenSelector { get; set; }

        [SerializeField, Interface(typeof(ISelector))]
        private UnityEngine.Object _closeSelector;
        private ISelector CloseSelector { get; set; }

        public bool Active { get; private set; } = false;

        protected bool _started;

        protected virtual void Awake()
        {
            OpenSelector = _openSelector as ISelector;
            CloseSelector = _closeSelector as ISelector;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(OpenSelector, nameof(OpenSelector));
            this.AssertField(CloseSelector, nameof(CloseSelector));
            this.EndStart(ref _started);
        }

        private void OnEnable()
        {
            if (_started)
            {
                OpenSelector.WhenSelected += HandleOpenSelected;
                CloseSelector.WhenSelected += HandleCloseSelected;
            }
        }

        private void OnDisable()
        {
            if (_started)
            {
                Active = false;
                OpenSelector.WhenSelected -= HandleOpenSelected;
                CloseSelector.WhenSelected -= HandleCloseSelected;
            }
        }

        private void HandleOpenSelected()
        {
            Active = true;
        }

        private void HandleCloseSelected()
        {
            Active = false;
        }

        #region Inject

        public void InjectAllActiveStateGate(ISelector openSelector, ISelector closeSelector)
        {
            InjectOpenState(openSelector);
            InjectCloseState(closeSelector);
        }

        public void InjectOpenState(ISelector openSelector)
        {
            _openSelector = openSelector as UnityEngine.Object;
            OpenSelector = openSelector;
        }

        public void InjectCloseState(ISelector closeSelector)
        {
            _closeSelector = closeSelector as UnityEngine.Object;
            CloseSelector = closeSelector;
        }

        #endregion
    }
}
