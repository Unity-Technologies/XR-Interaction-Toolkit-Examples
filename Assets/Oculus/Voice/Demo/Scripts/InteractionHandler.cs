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
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Voice.Demo
{
    public class InteractionHandler : MonoBehaviour
    {
        [Header("Default States"), Multiline]
        [SerializeField] private string freshStateText = "Try pressing the Activate button and saying \"Make the cube red\"";

        [Header("UI")]
        [SerializeField] private Text textArea;
        [SerializeField] private bool showJson;

        [Header("Voice")]
        [SerializeField] private AppVoiceExperience appVoiceExperience;

        // Whether voice is activated
        public bool IsActive => _active;
        private bool _active = false;

        // Add delegates
        private void OnEnable()
        {
            textArea.text = freshStateText;
            appVoiceExperience.VoiceEvents.OnSend.AddListener(OnSend);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.VoiceEvents.OnStartListening.AddListener(OnListenStart);
            appVoiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnListenStop);
            appVoiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.AddListener(OnListenForcedStop);
            appVoiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.AddListener(OnListenForcedStop);
            appVoiceExperience.VoiceEvents.OnResponse.AddListener(OnRequestResponse);
            appVoiceExperience.VoiceEvents.OnError.AddListener(OnRequestError);
        }
        // Remove delegates
        private void OnDisable()
        {
            appVoiceExperience.VoiceEvents.OnSend.RemoveListener(OnSend);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnListenStart);
            appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnListenStop);
            appVoiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.RemoveListener(OnListenForcedStop);
            appVoiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.RemoveListener(OnListenForcedStop);
            appVoiceExperience.VoiceEvents.OnResponse.RemoveListener(OnRequestResponse);
            appVoiceExperience.VoiceEvents.OnError.RemoveListener(OnRequestError);
        }

        // Request began
        private void OnSend(VoiceServiceRequest request)
        {
            // Store json on completion
            if (showJson && request is WitRequest witRequest) witRequest.onRawResponse = (response) => textArea.text = response;
            // Begin
            _active = true;
        }
        // Request transcript
        private void OnRequestTranscript(string transcript)
        {
            textArea.text = transcript;
        }
        // Listen start
        private void OnListenStart()
        {
            textArea.text = "Listening...";
        }
        // Listen stop
        private void OnListenStop()
        {
            textArea.text = "Processing...";
        }
        // Listen stop
        private void OnListenForcedStop()
        {
            if (!showJson)
            {
                textArea.text = freshStateText;
            }
            OnRequestComplete();
        }
        // Request response
        private void OnRequestResponse(WitResponseNode response)
        {
            if (!showJson)
            {
                if (!string.IsNullOrEmpty(response["text"]))
                {
                    textArea.text = "I heard: " + response["text"];
                }
                else
                {
                    textArea.text = freshStateText;
                }
            }
            OnRequestComplete();
        }
        // Request error
        private void OnRequestError(string error, string message)
        {
            if (!showJson)
            {
                textArea.text = $"<color=\"red\">Error: {error}\n\n{message}</color>";
            }
            OnRequestComplete();
        }
        // Deactivate
        private void OnRequestComplete()
        {
            _active = false;
        }

        // Toggle activation
        public void ToggleActivation()
        {
            SetActivation(!_active);
        }
        // Set activation
        public void SetActivation(bool toActivated)
        {
            if (_active != toActivated)
            {
                _active = toActivated;
                if (_active)
                {
                    appVoiceExperience.Activate();
                }
                else
                {
                    appVoiceExperience.Deactivate();
                }
            }
        }
    }
}
