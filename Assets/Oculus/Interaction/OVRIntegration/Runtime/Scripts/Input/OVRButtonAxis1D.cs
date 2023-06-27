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

namespace Oculus.Interaction
{
    /// <summary>
    /// Takes a set of OVR Near, Touch and Button bools and remaps them to an Axis1D float.
    /// </summary>
    public class OVRButtonAxis1D : MonoBehaviour, IAxis1D
    {
        [SerializeField]
        private OVRInput.Controller _controller;

        [SerializeField]
        private OVRInput.Button _near;

        [SerializeField]
        private OVRInput.Button _touch;

        [SerializeField]
        private OVRInput.Button _button;

        [SerializeField]
        private float _nearValue = 0.1f;

        [SerializeField]
        private float _touchValue = 0.5f;

        [SerializeField]
        private float _buttonValue = 1.0f;

        [SerializeField]
        private ProgressCurve _curve = new ProgressCurve(
            AnimationCurve.EaseInOut(0, 0, 1, 1),
            0.1f
        );

        #region Properties

        public float NearValue
        {
            get
            {
                return _nearValue;
            }
            set
            {
                _nearValue = value;
            }
        }

        public float TouchValue
        {
            get
            {
                return _touchValue;
            }
            set
            {
                _touchValue = value;
            }
        }

        public float ButtonValue
        {
            get
            {
                return _buttonValue;
            }
            set
            {
                _buttonValue = value;
            }
        }

        #endregion

        private float _baseValue = 0;
        private float _value = 0;
        private float _currentTarget = 0;

        public float Value()
        {
            return _value;
        }

        private float Target {
            get
            {
                if (OVRInput.Get(_button, _controller))
                {
                    return _buttonValue;
                }

                if (OVRInput.Get(_touch, _controller))
                {
                    return _touchValue;
                }

                if (OVRInput.Get(_near, _controller))
                {
                    return _nearValue;
                }

                return 0;
            }
        }

        protected virtual void Update()
        {
            float newTarget = Target;
            if (_currentTarget != newTarget)
            {
                _baseValue = _value;
                _currentTarget = newTarget;
                _curve.Start();
            }

            _value = _curve.Progress() * (_currentTarget - _baseValue);
        }

        #region Inject

        public void InjectAllOVRButtonAxis1D(OVRInput.Controller controller,
            OVRInput.Button near, OVRInput.Button touch, OVRInput.Button button)
        {
            _controller = controller;
            _near = near;
            _touch = touch;
            _button = button;
        }

        public void InjectOptionalCurve(ProgressCurve progressCurve)
        {
            _curve = progressCurve;
        }

        #endregion
    }
}
