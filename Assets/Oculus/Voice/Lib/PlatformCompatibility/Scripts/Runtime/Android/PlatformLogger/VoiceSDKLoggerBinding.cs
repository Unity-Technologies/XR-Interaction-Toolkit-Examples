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

namespace Oculus.Voice.Core.Bindings.Android.PlatformLogger
{
    public class VoiceSDKLoggerBinding : BaseServiceBinding
    {
        [UnityEngine.Scripting.Preserve]
        public VoiceSDKLoggerBinding(AndroidJavaObject loggerInstance) : base(loggerInstance) {}

        public void Connect()
        {
            binding.Call<bool>("connect");
        }

        public void LogInteractionStart(string requestId, string startTime)
        {
            binding.Call("logInteractionStart", requestId, startTime);
        }

        public void LogInteractionEndSuccess(string endTime)
        {
            binding.Call("logInteractionEndSuccess", endTime);
        }

        public void LogInteractionEndFailure(string endTime, string errorMessage)
        {
            binding.Call("logInteractionEndFailure", endTime, errorMessage);
        }

        public void LogInteractionPoint(string interactionPoint, string time)
        {
            binding.Call("logInteractionPoint", interactionPoint, time);
        }

        public void LogAnnotation(string annotationKey, string annotationValue)
        {
            binding.Call("logAnnotation", annotationKey, annotationValue);
        }
    }
}
