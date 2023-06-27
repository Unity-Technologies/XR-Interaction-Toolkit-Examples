/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data;
using UnityEngine;

namespace Meta.WitAi.Events
{
    [Serializable]
    public class AudioBufferEvents
    {
        public delegate void OnSampleReadyEvent(RingBuffer<byte>.Marker marker, float levelMax);
        public OnSampleReadyEvent OnSampleReady;

        [Tooltip("Called when the volume level of the mic input has changed")]
        public WitMicLevelChangedEvent OnMicLevelChanged = new WitMicLevelChangedEvent();

        [Header("Data")]
        public WitByteDataEvent OnByteDataReady = new WitByteDataEvent();
        public WitByteDataEvent OnByteDataSent = new WitByteDataEvent();
    }
}
