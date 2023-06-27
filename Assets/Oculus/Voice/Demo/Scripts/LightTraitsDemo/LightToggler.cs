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

using System;
using Meta.WitAi;
using Meta.WitAi.Json;
using UnityEngine;

namespace Oculus.Voice.Demo.LightTraitsDemo
{
    public class LightToggler : MonoBehaviour
    {
        public enum LightState
        {
            On,
            Off
        }

        private const string EMISSION_COLOR = "_EmissionColor";
        private const string EMISSION = "_EMISSION";

        private LightState _lightState;

        private Material _material;

        private Color _offColor = Color.black;
        private Color _onColor;

        // Start is called before the first frame update
        void Start()
        {
            _material = GetComponent<Renderer>().material;

            _onColor = _material.GetColor(EMISSION_COLOR);

            SetLightState((LightState.Off));
        }
        
        [MatchIntent("wit_change_state")]
        public void OnResponse(WitResponseNode commandResult)
        {
            var traitValue = commandResult.GetTraitValue("wit$on_off").Replace('o', 'O');

            SetLightState((LightState)Enum.Parse(typeof(LightState), traitValue));
        }

        public void SetLightState(LightState newState)
        {
            switch (newState)
            {
                case LightState.On:

                    if (_lightState == LightState.On)
                        break;

                    _material.EnableKeyword(EMISSION);

                    _material.SetColor(EMISSION_COLOR, _onColor);

                    break;

                case LightState.Off:

                    if (_lightState == LightState.Off)
                        break;

                    _material.DisableKeyword(EMISSION);

                    _material.SetColor(EMISSION_COLOR, _offColor);

                    break;
            }

            _lightState = newState;
        }
    }
}
