/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Facebook.WitAi.Events;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKListenerBinding : AndroidJavaProxy
    {
        private VoiceEvents voiceEvents;

        public VoiceEvents VoiceEvents => voiceEvents;

        public VoiceSDKListenerBinding(VoiceEvents voiceEvents) : base(
            "com.oculus.assistant.api.unity.dictation.UnityDictationListener")
        {
            this.voiceEvents = voiceEvents;
        }

        public void onResponse(string response)
        {
            voiceEvents.OnResponse?.Invoke(WitResponseNode.Parse(response));
        }

        public void onError(string error, string message)
        {
            voiceEvents.OnError?.Invoke(error, message);
        }

        public void onMicLevelChanged(float level)
        {
            voiceEvents.OnMicLevelChanged?.Invoke(level);
        }

        public void onRequestCreated()
        {
            voiceEvents.OnRequestCreated?.Invoke(null);
        }

        public void onStartListening()
        {
            voiceEvents.OnStartListening?.Invoke();
        }

        public void onStoppedListening()
        {
            voiceEvents.OnStoppedListening?.Invoke();
        }

        public void onStoppedListeningDueToInactivity()
        {
            voiceEvents.OnStoppedListeningDueToInactivity?.Invoke();
        }

        public void onStoppedListeningDueToTimeout()
        {
            voiceEvents.OnStoppedListeningDueToTimeout?.Invoke();
        }

        public void onStoppedListeningDueToDeactivation()
        {
            voiceEvents.OnStoppedListeningDueToDeactivation?.Invoke();
        }

        public void onMicDataSent()
        {
            voiceEvents.OnMicDataSent?.Invoke();
        }

        public void onMinimumWakeThresholdHit()
        {
            voiceEvents.OnMinimumWakeThresholdHit?.Invoke();
        }

        public void onPartialTranscription(string transcription)
        {
            voiceEvents.OnPartialTranscription?.Invoke(transcription);
        }

        public void onFullTranscription(string transcription)
        {
            voiceEvents.OnFullTranscription?.Invoke(transcription);
        }
    }
}
