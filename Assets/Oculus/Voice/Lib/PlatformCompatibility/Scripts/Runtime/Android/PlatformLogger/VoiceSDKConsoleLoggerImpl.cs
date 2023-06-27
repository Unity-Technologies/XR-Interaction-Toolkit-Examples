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


using Oculus.Voice.Core.Bindings.Interfaces;
using Oculus.Voice.Core.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Oculus.Voice.Core.Bindings.Android.PlatformLogger
{
    public class VoiceSDKConsoleLoggerImpl : IVoiceSDKLogger
    {
        public bool IsUsingPlatformIntegration { get; set; }
        public string WitApplication { get; set; }
        public bool ShouldLogToConsole { get; set; }
        private static readonly string TAG = "VoiceSDKConsoleLogger";

        private bool loggedFirstTranscriptionTime = false;
        public void LogInteractionStart(string requestId, string witApi)
        {
            if (!ShouldLogToConsole) return;
            loggedFirstTranscriptionTime = false;
            Debug.Log($"{TAG}: Interaction started with request ID: " + requestId);
            Debug.Log($"{TAG}: WitApi: " + witApi);
            Debug.Log($"{TAG}: request_start_time: " + DateTimeUtility.ElapsedMilliseconds.ToString());
            Debug.Log($"{TAG}: WitAppID: " + WitApplication);
            Debug.Log($"{TAG}: PackageName: " + Application.identifier);
        }

        public void LogInteractionEndSuccess()
        {
            if (!ShouldLogToConsole) return;
            Debug.Log($"{TAG}: Interaction finished successfully");
            Debug.Log($"{TAG}: request_end_time: " + DateTimeUtility.ElapsedMilliseconds.ToString());
        }

        public void LogInteractionEndFailure(string errorMessage)
        {
            if (!ShouldLogToConsole) return;
            Debug.Log($"{TAG}: Interaction finished with error: " + errorMessage);
            Debug.Log($"{TAG}: request_end_time: " + DateTimeUtility.ElapsedMilliseconds.ToString());
        }

        public void LogInteractionPoint(string interactionPoint)
        {
            if (!ShouldLogToConsole) return;
            Debug.Log($"{TAG}: Interaction point: " + interactionPoint);
            Debug.Log($"{TAG}: {interactionPoint}_start_time: " + DateTimeUtility.ElapsedMilliseconds.ToString());
        }

        public void LogAnnotation(string annotationKey, string annotationValue)
        {
            if (!ShouldLogToConsole) return;
            Debug.Log($"{TAG}: Logging key-value pair: {annotationKey}::{annotationValue}");
        }
        
        public void LogFirstTranscriptionTime()
        {
            if (!loggedFirstTranscriptionTime)
            {
                loggedFirstTranscriptionTime = true;
                LogInteractionPoint("firstPartialTranscriptionTime");
            }
        } 
    }
}
