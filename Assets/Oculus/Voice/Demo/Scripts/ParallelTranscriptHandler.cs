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

using Meta.WitAi;
using UnityEngine;

namespace Oculus.Voice.Demo.UIShapesDemo
{
    public class ParallelTranscriptHandler : MonoBehaviour
    {
        [Header("Transcript Requests")]
        [SerializeField] private string[] _requests;

        [Header("Voice")]
        [SerializeField] private VoiceService _voiceService;

        #if UNITY_EDITOR
        // Reset
        private string[] _activates = new string[] { "Set the", "Make the" };
        private string[] _shapes = new string[] { "cube", "sphere", "capsule", "cylinder", "pentagon" };
        private string[] _colors = new string[] { "red", "blue", "yellow", "green", "orange", "purple", "magenta", "cyan", "brown", "white", "black" };
        private void Reset()
        {
            int index = 0;
            _requests = new string[_activates.Length * _shapes.Length * _colors.Length];
            for (int a = 0; a < _activates.Length; a++)
            {
                for (int c = 0; c < _colors.Length; c++)
                {
                    for (int s = 0; s < _shapes.Length; s++)
                    {
                        _requests[index] = $"{_activates[a]} {_shapes[s]} {_colors[c]}";
                        index++;
                    }
                }
            }
        }
        #endif

        public void SendParallelRequests()
        {
            foreach (var request in _requests)
            {
                _voiceService.Activate(request);
            }
        }
    }
}
