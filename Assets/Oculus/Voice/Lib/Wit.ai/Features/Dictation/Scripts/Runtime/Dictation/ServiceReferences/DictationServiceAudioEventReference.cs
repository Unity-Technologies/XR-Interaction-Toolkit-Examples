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
    public class DictationServiceAudioEventReference : AudioInputServiceReference
    {
        [SerializeField] private DictationServiceReference _dictationServiceReference;

        public override IAudioInputEvents AudioEvents =>
            _dictationServiceReference.DictationService.AudioEvents;
    }
}
