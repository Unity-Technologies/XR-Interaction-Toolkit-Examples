/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Meta.WitAi.TTS.Samples
{
    public class TTSErrorText : MonoBehaviour
    {
        // Label
        [SerializeField] private Text _errorLabel;
        // Current error response
        private string _error = string.Empty;

        // Add listeners
        private void Update()
        {
            if (TTSService.Instance != null)
            {
                string invalidError = TTSService.Instance.GetInvalidError();
                if (!string.Equals(invalidError, _error))
                {
                    _error = invalidError;
                    if (string.IsNullOrEmpty(_error))
                    {
                        _errorLabel.text = string.Empty;
                    }
                    else
                    {
                        _errorLabel.text = $"TTS Service Error: {_error}";
                    }
                }
            }
        }
    }
}
