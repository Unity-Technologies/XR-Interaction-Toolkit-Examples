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
using Meta.WitAi.Events;
using Meta.WitAi.Json;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKListenerBinding : AndroidJavaProxy
    {
        private IVoiceService _voiceService;
        private readonly IVCBindingEvents _bindingEvents;

        public VoiceEvents VoiceEvents => _voiceService.VoiceEvents;
        public TelemetryEvents TelemetryEvents => _voiceService.TelemetryEvents;

        public enum StoppedListeningReason : int {
            NoReasonProvided = 0,
            Inactivity = 1,
            Timeout = 2,
            Deactivation = 3,
        }

        public VoiceSDKListenerBinding(IVoiceService voiceService, IVCBindingEvents bindingEvents) : base(
            "com.oculus.assistant.api.voicesdk.immersivevoicecommands.IVCEventsListener")
        {
            _voiceService = voiceService;
            _bindingEvents = bindingEvents;
        }

        public void onResponse(string responseJson)
        {
            WitResponseNode responseData = WitResponseNode.Parse(responseJson);
            if (responseData != null)
            {
                VoiceEvents.OnResponse?.Invoke(responseData);
            }
        }

        public void onPartialResponse(string responseJson)
        {
            WitResponseNode responseData = WitResponseNode.Parse(responseJson);
            if (responseData != null && responseData.HasResponse())
            {
                VoiceEvents.OnPartialResponse?.Invoke(responseData);
            }
        }

        public void onError(string error, string message, string errorBody)
        {
            VoiceEvents.OnError?.Invoke(error, message);
        }

        public void onAborted()
        {
            VoiceEvents.OnAborted?.Invoke();
        }

        public void onRequestCompleted()
        {
            VoiceEvents.OnRequestCompleted?.Invoke();
        }

        public void onMicLevelChanged(float level)
        {
            VoiceEvents.OnMicLevelChanged?.Invoke(level);
        }

        public void onRequestCreated()
        {
            #pragma warning disable CS0618
            VoiceEvents.OnRequestCreated?.Invoke(null);
            VoiceEvents.OnSend?.Invoke(null);
        }

        public void onStartListening()
        {
            VoiceEvents.OnStartListening?.Invoke();
        }

        public void onStoppedListening(int reason)
        {
            VoiceEvents.OnStoppedListening?.Invoke();
            switch((StoppedListeningReason)reason){
                case StoppedListeningReason.NoReasonProvided:
                    break;
                case StoppedListeningReason.Inactivity:
                    VoiceEvents.OnStoppedListeningDueToInactivity?.Invoke();
                    break;
                case StoppedListeningReason.Timeout:
                    VoiceEvents.OnStoppedListeningDueToTimeout?.Invoke();
                    break;
                case StoppedListeningReason.Deactivation:
                    VoiceEvents.OnStoppedListeningDueToDeactivation?.Invoke();
                    break;
            }
        }

        public void onMicDataSent()
        {
            VoiceEvents.OnMicDataSent?.Invoke();
        }

        public void onMinimumWakeThresholdHit()
        {
            VoiceEvents.OnMinimumWakeThresholdHit?.Invoke();
        }

        public void onPartialTranscription(string transcription)
        {
            VoiceEvents.OnPartialTranscription?.Invoke(transcription);
        }

        public void onFullTranscription(string transcription)
        {
            VoiceEvents.OnFullTranscription?.Invoke(transcription);
        }

        public void onServiceNotAvailable(string error, string message)
        {
            VLog.W($"Platform service is not available: {error} - {message}");
            _bindingEvents.OnServiceNotAvailable(error, message);
        }

        public void onAudioDurationTrackerFinished(long timestamp, double duration)
        {
            long ticksElapsed = NativeTimestampToDateTime(timestamp).Ticks / TimeSpan.TicksPerMillisecond;
            TelemetryEvents.OnAudioTrackerFinished?.Invoke(ticksElapsed, duration);
        }

        private DateTime NativeTimestampToDateTime(long javaTimestamp)
        {
            // Java timestamp is milliseconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddMilliseconds(javaTimestamp);
        }
    }
}
