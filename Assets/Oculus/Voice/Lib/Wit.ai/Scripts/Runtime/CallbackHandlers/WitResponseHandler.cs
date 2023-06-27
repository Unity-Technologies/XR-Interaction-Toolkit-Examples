/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Meta.WitAi.CallbackHandlers
{
    [Serializable]
    public class WitResponseEvent : UnityEvent<WitResponseNode> {}
    [Serializable]
    public class WitResponseErrorEvent : UnityEvent<WitResponseNode, string> {}

    public abstract class WitResponseHandler : MonoBehaviour
    {
        [FormerlySerializedAs("wit")]
        [SerializeField] public VoiceService Voice;
        [SerializeField] public bool ValidateEarly = false;

        // Whether validated early
        private bool _validated = false;

        private void OnValidate()
        {
            if (!Voice) Voice = FindObjectOfType<VoiceService>();
        }

        protected virtual void OnEnable()
        {
            if (!Voice) Voice = FindObjectOfType<VoiceService>();
            if (!Voice)
            {
                VLog.E($"VoiceService not found in scene.\nDisabling {GetType().Name} on {gameObject.name}");
                enabled = false;
            }
            else
            {
                Voice.VoiceEvents.OnSend.AddListener(OnRequestSend);
                Voice.VoiceEvents.OnValidatePartialResponse.AddListener(HandleValidateEarlyResponse);
                Voice.VoiceEvents.OnResponse.AddListener(HandleFinalResponse);
            }
        }

        protected virtual void OnDisable()
        {
            if (Voice)
            {
                Voice.VoiceEvents.OnSend.RemoveListener(OnRequestSend);
                Voice.VoiceEvents.OnValidatePartialResponse.RemoveListener(HandleValidateEarlyResponse);
                Voice.VoiceEvents.OnResponse.RemoveListener(HandleFinalResponse);
            }
        }

        protected virtual void OnRequestSend(VoiceServiceRequest request)
        {
            _validated = false;
        }

        protected virtual void HandleValidateEarlyResponse(VoiceSession session)
        {
            if (ValidateEarly && !_validated)
            {
                string invalidReason = OnValidateResponse(session.response, true);
                if (string.IsNullOrEmpty(invalidReason))
                {
                    _validated = true;
                    OnResponseSuccess(session.response);
                    session.validResponse = true;
                }
            }
        }
        protected virtual void HandleFinalResponse(WitResponseNode response)
        {
            // Not yet handled
            if (!_validated)
            {
                string invalidReason = OnValidateResponse(response, false);
                if (!string.IsNullOrEmpty(invalidReason))
                {
                    OnResponseInvalid(response, invalidReason);
                }
                else
                {
                    OnResponseSuccess(response);
                }
                _validated = true;
            }
        }

        // Handle validation
        protected abstract string OnValidateResponse(WitResponseNode response, bool isEarlyResponse);
        // Handle invalid
        protected abstract void OnResponseInvalid(WitResponseNode response, string error);
        // Handle valid
        protected abstract void OnResponseSuccess(WitResponseNode response);

        #if UNITY_EDITOR
        // For tests
        public void HandleResponse(WitResponseNode response) => HandleFinalResponse(response);
        #endif

        /// <summary>
        /// Refresh confidence range
        /// </summary>
        /// <param name="confidence"></param>
        /// <param name="confidenceRanges"></param>
        /// <param name="allowConfidenceOverlap"></param>
        /// <returns></returns>
        public static bool RefreshConfidenceRange(float confidence, ConfidenceRange[] confidenceRanges, bool allowConfidenceOverlap)
        {
            // Whether found
            bool foundIn = false;
            bool foundOut = false;

            // Iterate confidences
            for (int i = 0; null != confidenceRanges && i < confidenceRanges.Length; i++)
            {
                var range = confidenceRanges[i];

                // Within Range
                if (confidence >= range.minConfidence &&
                    confidence <= range.maxConfidence)
                {
                    // Ignore overlap
                    if (!allowConfidenceOverlap && foundIn)
                    {
                        continue;
                    }

                    // Within delegate
                    range.onWithinConfidenceRange?.Invoke();
                    foundIn = true;
                }
                // Out of Range
                else
                {
                    // Ignore overlap
                    if (!allowConfidenceOverlap && foundOut)
                    {
                        continue;
                    }

                    // Outside delegate
                    range.onOutsideConfidenceRange?.Invoke();
                    foundOut = true;
                }
            }

            // Return if found within
            return foundIn;
        }
    }
}
