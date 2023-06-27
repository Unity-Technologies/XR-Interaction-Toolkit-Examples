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

using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class UseFingerRawPinchAPI : MonoBehaviour, IFingerUseAPI
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        private IHand Hand { get; set; }

        private IFingerAPI _grabAPI = new FingerRawPinchAPI();

        private int _lastDataVersion = -1;
        protected bool _started;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.EndStart(ref _started);
        }

        public float GetFingerUseStrength(HandFinger finger)
        {
            if (_lastDataVersion != Hand.CurrentDataVersion)
            {
                _lastDataVersion = Hand.CurrentDataVersion;
                _grabAPI.Update(Hand);
            }
            return _grabAPI.GetFingerGrabScore(finger);
        }

        #region Inject
        public void InjectAllUseFingerRawPinchAPI(IHand hand)
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
