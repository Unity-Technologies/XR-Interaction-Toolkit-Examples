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
using Meta.WitAi;
using Meta.WitAi.Dictation;
using Meta.WitAi.Dictation.Data;
using Meta.WitAi.Dictation.Events;

namespace Oculus.Voice.Dictation.Bindings.Android
{
    public class DictationListenerBinding : AndroidJavaProxy
    {
        private IDictationService _dictationService;
        private IServiceEvents _serviceEvents;
        private DictationEvents DictationEvents => _dictationService.DictationEvents;

        public DictationListenerBinding(IDictationService dictationService, IServiceEvents serviceEvents)
            : base("com.oculus.assistant.api.voicesdk.dictation.PlatformDictationListener")
        {
            _dictationService = dictationService;
            _serviceEvents = serviceEvents;
        }

        public void onStart(string sessionId)
        {
            DictationEvents.OnStartListening?.Invoke();
            DictationSession session = new PlatformDictationSession()
            {
                dictationService = _dictationService,
                platformSessionId = sessionId
            };
        }

        public void onMicAudioLevel(string sessionId, int micLevel)
        {
            DictationEvents.OnMicAudioLevelChanged?.Invoke(micLevel / 100.0f);
        }

        public void onPartialTranscription(string sessionId, string transcription)
        {
            DictationEvents.OnPartialTranscription?.Invoke(transcription);
        }

        public void onFinalTranscription(string sessionId, string transcription)
        {
            DictationEvents.OnFullTranscription?.Invoke(transcription);
        }

        public void onError(string sessionId, string errorType, string errorMessage)
        {
            DictationEvents.OnError?.Invoke(errorType, errorMessage);
        }

        public void onStopped(string sessionId)
        {
            DictationEvents.OnStoppedListening?.Invoke();
            DictationSession session = new PlatformDictationSession()
            {
                dictationService = _dictationService,
                platformSessionId = sessionId
            };
        }

        public void onServiceNotAvailable(string error, string message)
        {
            VLog.W("Platform dictation service is not available");
            _serviceEvents.OnServiceNotAvailable(error, message);
        }
    }
}
