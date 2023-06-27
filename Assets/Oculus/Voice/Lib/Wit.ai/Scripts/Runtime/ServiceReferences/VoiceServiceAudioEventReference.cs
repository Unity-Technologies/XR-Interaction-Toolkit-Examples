/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Interfaces;
using Meta.WitAi.Utilities;
using UnityEngine;

namespace Meta.WitAi.ServiceReferences
{
    public class VoiceServiceAudioEventReference : AudioInputServiceReference
    {
        [SerializeField] private VoiceServiceReference _voiceServiceReference;
        public override IAudioInputEvents AudioEvents => _voiceServiceReference.VoiceService.AudioEvents;
    }
}
