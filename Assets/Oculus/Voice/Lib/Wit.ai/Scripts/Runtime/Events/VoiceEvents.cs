/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.WitAi.Events
{
    [Serializable]
    public class VoiceEvents : SpeechEvents
    {
        private const string EVENT_CATEGORY_DATA_EVENTS = "Data Events";

        [EventCategory(EVENT_CATEGORY_DATA_EVENTS)]
        [FormerlySerializedAs("OnByteDataReady")] [SerializeField] [HideInInspector]
        private WitByteDataEvent _onByteDataReady = new WitByteDataEvent();
        public WitByteDataEvent OnByteDataReady => _onByteDataReady;

        [EventCategory(EVENT_CATEGORY_DATA_EVENTS)]
        [FormerlySerializedAs("OnByteDataSent")] [SerializeField] [HideInInspector]
        private WitByteDataEvent _onByteDataSent = new WitByteDataEvent();
        public WitByteDataEvent OnByteDataSent => _onByteDataSent;

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESPONSE)]
        [Tooltip("Called after an on partial response to validate data.  If data.validResponse is true, service will deactivate & use the partial data as final")]
        [FormerlySerializedAs("OnValidatePartialResponse")] [SerializeField]
        private WitValidationEvent _onValidatePartialResponse = new WitValidationEvent();
        public WitValidationEvent OnValidatePartialResponse => _onValidatePartialResponse;
    }
 }
