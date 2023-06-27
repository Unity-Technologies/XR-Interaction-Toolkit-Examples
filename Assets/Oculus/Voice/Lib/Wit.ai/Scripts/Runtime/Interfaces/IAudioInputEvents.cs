/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Events;
using UnityEngine.Events;

namespace Meta.WitAi.Interfaces
{
    public interface IAudioInputEvents
    {
        WitMicLevelChangedEvent OnMicAudioLevelChanged { get; }
        UnityEvent OnMicStartedListening { get; }
        UnityEvent OnMicStoppedListening { get; }
    }
}
