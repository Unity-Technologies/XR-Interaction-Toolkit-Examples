/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine.Events;

namespace Meta.Voice
{
    [Serializable]
    public class NLPRequestEvents<TUnityEvent>
        : NLPAudioRequestEvents<TUnityEvent>,
            INLPTextRequestEvents<TUnityEvent>
        where TUnityEvent : UnityEventBase
    {

    }
}
