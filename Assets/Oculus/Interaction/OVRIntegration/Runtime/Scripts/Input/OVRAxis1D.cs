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

namespace Oculus.Interaction.OVR.Input
{
    public class OVRAxis1D : MonoBehaviour, IAxis1D
    {
        [SerializeField]
        private OVRInput.Controller _controller;

        [SerializeField]
        private OVRInput.Axis1D _axis1D;

        [SerializeField]
        private RemapConfig _remapConfig = new RemapConfig()
        {
            Enabled = false,
            Curve = AnimationCurve.Linear(0,0,1,1)
        };

        [Serializable]
        public class RemapConfig
        {
            public bool Enabled;
            public AnimationCurve Curve;
        }

        public float Value()
        {
            float value = OVRInput.Get(_axis1D, _controller);
            if (_remapConfig.Enabled)
            {
                value = _remapConfig.Curve.Evaluate(value);
            }

            return value;
        }
    }
}
