// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// Source: https://github.com/adrenak/unimic/blob/master/Assets/UniMic/Runtime/Mic.cs

using System;
using UnityEngine;
using Meta.WitAi.Data;
using Meta.WitAi.Interfaces;

namespace Meta.WitAi.Lib
{
    #if UNITY_EDITOR
    public class WebGlMic : MonoBehaviour, IAudioInputSource
    #else
    public class Mic : MonoBehaviour, IAudioInputSource
    #endif
    {
#pragma warning disable 0067
        public event Action OnStartRecording;
        public event Action OnStartRecordingFailed;
#pragma warning disable 0067
        public event Action<int, float[], float> OnSampleReady;
        public event Action OnStopRecording;
        public void StartRecording(int sampleLen)
        {
            VLog.E("Direct microphone use is not currently supported in WebGL.");
            OnStartRecordingFailed?.Invoke();
        }

        public void StopRecording()
        {
            OnStopRecording?.Invoke();
        }

        public bool IsRecording => false;
        public AudioEncoding AudioEncoding => new AudioEncoding();
        public bool IsInputAvailable => false;
        public void CheckForInput()
        {

        }
        private bool MicrophoneIsRecording(string device)
        {
            return false;
        }

        private string[] MicrophoneGetDevices()
        {
            VLog.E("Direct microphone use is not currently supported in WebGL.");
            return new string[] {};
        }

        private int MicrophoneGetPosition(string device)
        {
            // This should (probably) never happen, since the Start/Stop Recording methods will
            // silently fail under webGL.
            return 0;
        }
    }
}
