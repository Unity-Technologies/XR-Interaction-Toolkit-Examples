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

using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{

    public class PressureSquishable : MonoBehaviour, IHandGrabUseDelegate
    {
        [SerializeField]
        private GameObject _squishableObject;

        [SerializeField] [Range(0.01f, 1)]
        private float _maxSquish = 0.25f;
        [SerializeField] [Range(0.01f, 1)]
        private float _maxStretch = 0.15f;

        protected bool _started;
        private Vector3 _initialScale;
        protected virtual void Start()
        {
            this.AssertField(_squishableObject, nameof(_squishableObject));

            _initialScale = _squishableObject.transform.localScale;
        }

        public void BeginUse()
        {
        }

        public void EndUse()
        {
            _squishableObject.transform.localScale = _initialScale;
        }

        public float ComputeUseStrength(float strength)
        {
            float squishAmount = Mathf.Lerp(1, 1 - _maxSquish, strength);
            float stretchAmount = Mathf.Lerp(1, 1 + _maxStretch, strength);

            // Perform a cheap axis squish and stretch effect
            _squishableObject.transform.localScale = new Vector3(_initialScale.x * stretchAmount, _initialScale.y * squishAmount, _initialScale.z * stretchAmount);
            return strength;
        }

    }

}
