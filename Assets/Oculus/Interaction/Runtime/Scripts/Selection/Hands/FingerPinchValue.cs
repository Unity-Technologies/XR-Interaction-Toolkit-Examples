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
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class FingerPinchValue : MonoBehaviour, IAxis1D
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private HandFinger _finger = HandFinger.Index;
        public HandFinger Finger
        {
            get
            {
                return _finger;
            }
            set
            {
                _finger = value;
            }
        }

        [SerializeField, Range(0f, 1f)]
        private float _changeRate = 1;
        public float ChangeRate
        {
            get
            {
                return _changeRate;
            }
            private set
            {
                _changeRate = value;
            }
        }

        [SerializeField]
        private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve Curve
        {
            get
            {
                return _curve;
            }
            set
            {
                _curve = value;
            }
        }


        private float _value = 0;

        protected bool _started = false;
        private bool _firstCall;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Hand);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _firstCall = true;
                Hand.WhenHandUpdated += HandleHandUpdated;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandUpdated;
            }
        }

        public float Value()
        {
            return _value;
        }

        private void HandleHandUpdated()
        {
            float value = Hand.GetFingerPinchStrength(Finger);
            value = Curve.Evaluate(value);
            if (_firstCall)
            {
                _firstCall = false;
                _value = value;
            }
            else
            {
                _value = Mathf.Lerp(_value, value, _changeRate);
            }
        }

        #region Inject

        public void InjectAllFingerPinchValue(IHand hand)
        {
            InjectHand(hand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        #endregion
    }
}
