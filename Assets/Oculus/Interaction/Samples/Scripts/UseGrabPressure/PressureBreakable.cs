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
using System.Collections;

namespace Oculus.Interaction
{

    public class PressureBreakable : MonoBehaviour, IHandGrabUseDelegate
    {
        [SerializeField] [Range(0, 1)]
        private float _breakThreshold = 0.9f;

        [SerializeField]
        private GameObject _unbrokenObject = null;

        [SerializeField]
        private GameObject _brokenObject = null;

        [SerializeField]
        private Rigidbody[] _brokenBodies = null;

        [SerializeField]
        private HandGrabInteractable[] _grabInteractables = null;

        [Header("Break Effects")]
        [SerializeField]
        private float _explosionForce = 3;
        [SerializeField]
        private float _explosionRadius = 0.5f;

        [SerializeField]
        private float _unbreakDelay = 3;

        private float _useStrength = 0;
        private bool _isBroken = false;
        private Pose[] _brokenBodiesInitialPoses = null;

        protected virtual void Awake()
        {
            // Set initial state
            _unbrokenObject.SetActive(!_isBroken);
            _brokenObject.SetActive(_isBroken);
        }

        protected virtual void Start()
        {
            this.AssertField(_unbrokenObject, nameof(_unbrokenObject));
            this.AssertField(_brokenObject, nameof(_brokenObject));

            // Setup broken bodies
            _brokenBodiesInitialPoses = new Pose[_brokenBodies.Length];
            for (int i = 0; i < _brokenBodies.Length; ++i)
            {
                Rigidbody brokenBody = _brokenBodies[i];
                _brokenBodiesInitialPoses[i] = new Pose(brokenBody.transform.localPosition, brokenBody.transform.localRotation);
            }
        }

        protected virtual void Update()
        {
            if (_useStrength >= _breakThreshold)
            {
                Break();
            }
        }

        public void BeginUse()
        {
        }

        public void EndUse()
        {
            _useStrength = 0;
        }

        public float ComputeUseStrength(float strength)
        {
            _useStrength = strength;
            return _useStrength;
        }

        private void Break()
        {
            if (_isBroken)
            {
                return;
            }

            // Hide the unbroken object
            _isBroken = true;
            _unbrokenObject.SetActive(!_isBroken);

            // Disable grabbing
            foreach (var grabInteractable in _grabInteractables)
            {
                grabInteractable.Disable();
            }

            // Show the broken object
            _brokenObject.SetActive(_isBroken);
            foreach (var breakableBody in _brokenBodies)
            {
                breakableBody.mass = 1 / (float)_brokenBodies.Length;
                breakableBody.AddExplosionForce(_explosionForce, this.transform.position, _explosionRadius);
            }

            this.StartCoroutine(Unbreak());
        }

        private IEnumerator Unbreak()
        {
            if (!_isBroken)
            {
                yield break;
            }

            yield return new WaitForSeconds(_unbreakDelay);

            // Hide and reset the broken object
            _isBroken = false;
            _brokenObject.SetActive(_isBroken);

            for (int i = 0; i < _brokenBodies.Length; ++i)
            {
                Rigidbody brokenBody = _brokenBodies[i];
                Pose brokenInitialPose = _brokenBodiesInitialPoses[i];

                // Reset
                brokenBody.velocity = Vector3.zero;
                brokenBody.angularVelocity = Vector3.zero;
                brokenBody.transform.localPosition = brokenInitialPose.position;
                brokenBody.transform.localRotation = brokenInitialPose.rotation;
            }

            // Enable grabbing
            foreach (var grabInteractable in _grabInteractables)
            {
                grabInteractable.Enable();
            }

            // Show the unbroken object
            _unbrokenObject.SetActive(!_isBroken);
        }

    }

}
