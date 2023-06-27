/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */


using System;
namespace Meta.WitAi.Events
{
    [Flags]
    public enum VoiceState
    {
        MicOff = 1,   //000001
        MicOn = 2,    //000010
        Listening = 4,//000100
        StartProcessing = 8,//001000
        Response = 16,//010000
        Error = 32, //100000
    }

}
