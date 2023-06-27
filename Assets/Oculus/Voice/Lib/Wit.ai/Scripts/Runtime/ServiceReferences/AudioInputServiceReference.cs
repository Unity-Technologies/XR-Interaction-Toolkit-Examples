/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Interfaces;
using UnityEngine;

namespace Meta.WitAi.ServiceReferences
{
    public abstract class AudioInputServiceReference : MonoBehaviour, IAudioEventProvider
    {
        public abstract IAudioInputEvents AudioEvents { get; }
    }
}
