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
using UnityEngine.Events;
using TMPro;

namespace Oculus.Interaction.Body.Samples
{
    public class PoseCaptureCountdown : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent _timerStart = new UnityEvent();

        [SerializeField]
        private UnityEvent _timerSecondTick = new UnityEvent();

        [SerializeField]
        private UnityEvent _timeUp = new UnityEvent();

        [SerializeField]
        private TextMeshProUGUI _countdownText;

        [SerializeField]
        private string _poseText = "Capture Pose";

        [SerializeField]
        private float duration = 10f;

        [SerializeField, Optional]
        private Renderer _renderer;

        [SerializeField, Optional]
        private Color _resetColor;

        private float _timer = 0f;

        public void Restart()
        {
            _timer = duration;
            _timerStart.Invoke();
            if (_renderer != null)
            {
                _renderer.material.color = _resetColor;
            }
        }

        private void Update()
        {
            bool wasCounting = _timer > 0f;
            if (wasCounting)
            {
                int prevSecond = (int)_timer;
                _timer -= Time.unscaledDeltaTime;
                if ((int)_timer < prevSecond)
                {
                    _timerSecondTick.Invoke();
                }
            }
            bool isCounting = _timer > 0f;

            if (wasCounting && !isCounting)
            {
                _timer = 0f;
                _timeUp.Invoke();
                _countdownText.text = _poseText;
            }
            else if (isCounting)
            {
                _countdownText.text = _timer.ToString("#0.0");
            }
        }
    }
}
