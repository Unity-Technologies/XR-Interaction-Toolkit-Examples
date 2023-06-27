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
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Provides finger use strength values driven by an Axis1D.
    /// </summary>
    public class Axis1DFingerUseAPI : MonoBehaviour, IFingerUseAPI
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;

        [FormerlySerializedAs("_pressureAxis")]
        [FormerlySerializedAs("_pinchPressure")]
        [SerializeField, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _axis;

        protected IHand Hand;
        protected IAxis1D Axis;

        protected bool _started;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            Axis = _axis as IAxis1D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(Axis, nameof(Axis));
            this.EndStart(ref _started);
        }

        public float GetFingerUseStrength(HandFinger finger)
        {
            return Hand.GetFingerIsPinching(finger) ? Axis.Value() : 0;
        }

#region Inject
        public void InjectAllUseFingerPinchPressureApi(IHand hand, IAxis1D axis)
        {
            InjectHand(hand);
            InjectAxis(axis);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectAxis(IAxis1D pinchPressure)
        {
            Axis = pinchPressure;
            _axis = pinchPressure as UnityEngine.Object;
        }
#endregion
    }
}
