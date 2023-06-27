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
    public class VoiceSDKPlatformLoggerImpl : BaseAndroidConnectionImpl<VoiceSDKLoggerBinding>, IVoiceSDKLogger
    {
        public bool IsUsingPlatformIntegration { get; set; }
        public string WitApplication { get; set; }
        public bool ShouldLogToConsole
        {
            get => consoleLoggerImpl.ShouldLogToConsole;
            set => consoleLoggerImpl.ShouldLogToConsole = value;
        }
        private VoiceSDKConsoleLoggerImpl consoleLoggerImpl = new VoiceSDKConsoleLoggerImpl();
        public VoiceSDKPlatformLoggerImpl() : base(
            "com.oculus.assistant.api.unity.logging.UnityPlatformLoggerServiceFragment")
        {
        }

        private bool loggedFirstTranscriptionTime = false;

        public override void Connect(string version)
        {
            base.Connect(version);
            service.Connect();
            Debug.Log(
                $"Logging Platform integration initialization complete.");
        }

        public override void Disconnect()
        {
            Debug.Log("Logging Platform integration shutdown");
            base.Disconnect();
        }

        public void LogInteractionStart(string requestId, string witApi)
        {
            loggedFirstTranscriptionTime = false;
            consoleLoggerImpl.LogInteractionStart(requestId, witApi);
            service.LogInteractionStart(requestId, DateTimeUtility.ElapsedMilliseconds.ToString());
            LogAnnotation("isUsingPlatform", IsUsingPlatformIntegration.ToString());
            LogAnnotation("witApi", witApi);
            LogAnnotation("witAppId", WitApplication);
            LogAnnotation("package", Application.identifier);
        }

        public void LogInteractionEndSuccess()
        {
            consoleLoggerImpl.LogInteractionEndSuccess();
            service.LogInteractionEndSuccess(DateTimeUtility.ElapsedMilliseconds.ToString());
        }

        public void LogInteractionEndFailure(string errorMessage)
        {
            consoleLoggerImpl.LogInteractionEndFailure(errorMessage);
            service.LogInteractionEndFailure(DateTimeUtility.ElapsedMilliseconds.ToString(), errorMessage);
        }

        public void LogInteractionPoint(string interactionPoint)
        {
            consoleLoggerImpl.LogInteractionPoint(interactionPoint);
            service.LogInteractionPoint(interactionPoint, DateTimeUtility.ElapsedMilliseconds.ToString());
        }

        public void LogAnnotation(string annotationKey, string annotationValue)
        {
            consoleLoggerImpl.LogAnnotation(annotationKey, annotationValue);
            service.LogAnnotation(annotationKey, annotationValue);
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
