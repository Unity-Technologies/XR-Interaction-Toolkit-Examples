/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data;

namespace Meta.WitAi
{
    public class AudioDurationTracker
    {
        private readonly String _requestId;
        private double _bytesCaptured = 0.0;
        private readonly int _bytesPerSample;
        private readonly AudioEncoding _audioEncoding;
        private long _finalizeTimeStamp;
        private double _audioDurationMs;


        public AudioDurationTracker(string requestId, AudioEncoding audioEncoding)
        {
            _requestId = requestId;
            _audioEncoding = audioEncoding;
            _bytesPerSample = _audioEncoding.bits / 8;
        }

        public void AddBytes(long bytes)
        {
            _bytesCaptured += bytes;
        }

        public void FinalizeAudio()
        {
            _finalizeTimeStamp = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
            _audioDurationMs =
                (_bytesCaptured / (_audioEncoding.samplerate * _audioEncoding.numChannels * _bytesPerSample)) * 1000.0;
        }

        public long GetFinalizeTimeStamp()
        {
            return _finalizeTimeStamp;
        }

        public double GetAudioDuration()
        {
            return _audioDurationMs;
        }

        public string GetRequestId()
        {
            return _requestId;
        }
    }
}
