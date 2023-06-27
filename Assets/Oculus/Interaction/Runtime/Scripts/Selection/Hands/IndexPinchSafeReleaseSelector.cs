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
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This Selector selects for a frame when the Pinch is released, as opposed to when it is pinching.
    /// It uses the system pinch (index and thumb) but due to some false-positives with pinch detection,
    /// to establish that a pinch has released the index must be not pinching and be extended.
    /// </summary>
    public class IndexPinchSafeReleaseSelector : MonoBehaviour,
        ISelector, IActiveState
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private bool _selectOnRelease = true;

        public bool Active => this.enabled && _isIndexFingerPinching && !_cancelled;

        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _indexReleaseSafeguard;
        private IActiveState IndexReleaseSafeguard;

        private bool _isIndexFingerPinching;
        private bool _cancelled;
        private bool _pendingUnselect;

        public event Action WhenSelected = delegate { };
        public event Action WhenUnselected = delegate { };

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            IndexReleaseSafeguard = _indexReleaseSafeguard as IActiveState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandUpdated;
                _isIndexFingerPinching = false;
                _pendingUnselect = false;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        private void HandleHandUpdated()
        {
            bool isPinchDetected = Hand.GetIndexFingerIsPinching();
            bool isReleaseDetected = !isPinchDetected && IndexReleaseSafeguard.Active;

            if (_selectOnRelease)
            {
                ProcessSelectOnRelease(isPinchDetected, isReleaseDetected);
            }
            else
            {
                ProcessSelectOnPinch(isPinchDetected, isReleaseDetected);
            }
        }

        private void ProcessSelectOnPinch(bool isPinchDetected, bool isReleaseDetected)
        {
            if (!_isIndexFingerPinching && isPinchDetected && !_cancelled)
            {
                _isIndexFingerPinching = true;
                WhenSelected.Invoke();
            }
            else if (_isIndexFingerPinching && isReleaseDetected)
            {
                _isIndexFingerPinching = false;
                WhenUnselected.Invoke();
            }

            if (!_isIndexFingerPinching && _cancelled)
            {
                _cancelled = false;
            }
        }

        private void ProcessSelectOnRelease(bool isPinchDetected, bool isReleaseDetected)
        {
            if (!_isIndexFingerPinching && isPinchDetected)
            {
                _isIndexFingerPinching = true;
            }

            if (!_isIndexFingerPinching && _pendingUnselect)
            {
                _pendingUnselect = false;
                WhenUnselected.Invoke();
            }

            if (_isIndexFingerPinching && isReleaseDetected)
            {
                _isIndexFingerPinching = false;
                if (!_cancelled)
                {
                    WhenSelected.Invoke();
                    _pendingUnselect = true;
                }
                _cancelled = false;
            }
        }

        public void Cancel()
        {
            if (_isIndexFingerPinching)
            {
                _cancelled = true;
            }
        }

        #region Inject

        public void InjectAllIndexPinchSafeReleaseSelector(IHand hand,
            bool selectOnRelease,
            IActiveState indexReleaseSafeguard)
        {
            InjectHand(hand);
            InjectSelectOnRelease(selectOnRelease);
            InjectIndexReleaseSafeguard(indexReleaseSafeguard);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectSelectOnRelease(bool selectOnRelease)
        {
            _selectOnRelease = selectOnRelease;
        }

        public void InjectIndexReleaseSafeguard(IActiveState indexReleaseSafeguard)
        {
            _indexReleaseSafeguard = indexReleaseSafeguard as UnityEngine.Object;
            IndexReleaseSafeguard = indexReleaseSafeguard;
        }
        #endregion
    }
}
