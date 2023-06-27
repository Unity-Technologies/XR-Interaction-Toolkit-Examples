/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;

namespace Meta.WitAi.TTS.Samples
{
    public class TTSSpeakerInput : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private InputField _input;
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _speakButton;
        [SerializeField] private Button _speakQueuedButton;

        [SerializeField] private string _dateId = "[DATE]";
        [SerializeField] private string[] _queuedText;

        // States
        private bool _loading;
        private bool _speaking;

        // Add delegates
        private void OnEnable()
        {
            RefreshButtons();
            _stopButton.onClick.AddListener(StopClick);
            _speakButton.onClick.AddListener(SpeakClick);
            _speakQueuedButton.onClick.AddListener(SpeakQueuedClick);
        }
        // Stop click
        private void StopClick() => _speaker.Stop();
        // Speak phrase click
        private void SpeakClick() => _speaker.Speak(FormatText(_input.text));
        // Speak queued phrase click
        private void SpeakQueuedClick()
        {
            foreach (var text in _queuedText)
            {
                _speaker.SpeakQueued(FormatText(text));
            }
            _speaker.SpeakQueued(FormatText(_input.text));
        }
        // Format text with current datetime
        private string FormatText(string text)
        {
            string result = text;
            if (result.Contains(_dateId))
            {
                DateTime now = DateTime.Now;
                string dateString = $"{now.ToLongDateString()} at {now.ToShortTimeString()}";
                result = text.Replace(_dateId, dateString);
            }
            return result;
        }
        // Remove delegates
        private void OnDisable()
        {
            _stopButton.onClick.RemoveListener(StopClick);
            _speakButton.onClick.RemoveListener(SpeakClick);
            _speakQueuedButton.onClick.RemoveListener(SpeakQueuedClick);
        }

        // Preset text fields
        private void Update()
        {
            // On preset voice id update
            if (!string.Equals(_title.text, _speaker.presetVoiceID))
            {
                _title.text = _speaker.presetVoiceID;
                _input.placeholder.GetComponent<Text>().text = $"Write something to say in {_speaker.presetVoiceID}'s voice";
            }
            // On state changes
            if (_loading != _speaker.IsLoading)
            {
                _loading = _speaker.IsLoading;
                RefreshButtons();
            }
            if (_speaking != _speaker.IsSpeaking)
            {
                _speaking = _speaker.IsSpeaking;
                RefreshButtons();
            }
        }
        // Refresh interactable based on states
        private void RefreshButtons()
        {
            _stopButton.interactable = _loading || _speaking;
        }
    }
}
