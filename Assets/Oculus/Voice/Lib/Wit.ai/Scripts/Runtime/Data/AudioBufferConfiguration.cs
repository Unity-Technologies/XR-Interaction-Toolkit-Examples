/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Data
{
    [Serializable]
    public class AudioBufferConfiguration
    {
        [Tooltip("The length of the individual samples read from the audio source")]
        [Range(10, 500)]
        [SerializeField]
        public int sampleLengthInMs = 10;

        [Tooltip(
            "The total audio data that should be buffered for lookback purposes on sound based activations.")]
        [SerializeField]
        public float micBufferLengthInSeconds = 1;
    }
}
