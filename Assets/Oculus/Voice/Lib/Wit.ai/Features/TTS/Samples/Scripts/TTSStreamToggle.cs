/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Integrations;

namespace Meta.WitAi.TTS.Samples
{
    public class TTSStreamToggle : MonoBehaviour
    {
        // UI references
        [SerializeField] private TTSWit _service;
        [SerializeField] private Text _label;
        [SerializeField] private Button _button;

        // Current stream
        private bool _streamEnabled = true;

        // Add listeners
        private void OnEnable()
        {
            // Obtain disk cache if possible
            if (_service == null)
            {
                _service = GameObject.FindObjectOfType<TTSWit>();
            }
            // Log for missing service
            if (_service == null)
            {
                VLog.E("TTS Stream Toggle - Cannot work without a TTSWit reference");
            }
            // Reset
            RefreshStreaming();
            _button.onClick.AddListener(ToggleStreaming);
        }
        // Remove listeners
        private void OnDisable()
        {
            _button.onClick.RemoveListener(ToggleStreaming);
        }

        // Refresh location & button text
        private void RefreshStreaming()
        {
            _streamEnabled = GetStreaming();
            _label.text = $"Streaming: {(_streamEnabled ? "ON" : "OFF")}";
        }
        // Toggle streaming
        public void ToggleStreaming()
        {
            SetStreaming(!_streamEnabled);
            RefreshStreaming();
        }

        // Get streaming option from service
        private bool GetStreaming()
        {
            return _service && _service.RequestSettings.audioStream;
        }
        // Set streaming option to
        private void SetStreaming(bool toStreaming)
        {
            if (_service != null)
            {
                _service.RequestSettings.audioStream = toStreaming;
            }
        }
        // Update if changed externally
        private void Update()
        {
            if (_streamEnabled != GetStreaming())
            {
                RefreshStreaming();
            }
        }
    }
}
