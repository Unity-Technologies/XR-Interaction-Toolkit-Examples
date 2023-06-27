/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    // Audio stream support type
    public enum AudioStreamDecodeType
    {
        PCM16,
        MP3
    }
    // Data used to handle stream
    public struct AudioStreamData
    {
        // Generated clip name
        public string ClipName;
        // Amount of clip length in seconds that must be received before stream is considered ready.
        public float ClipReadyLength;
        // Total samples to be used to generate clip. A new clip will be generated every time this chunk size is surpassed
        public int ClipChunkSize;

        // Type of audio code to be decoded
        public AudioStreamDecodeType DecodeType;
        // Total channels being streamed
        public int DecodeChannels;
        // Samples per second being streamed
        public int DecodeSampleRate;
    }

    // Audio stream handler
    public class AudioStreamHandler : DownloadHandlerScript, IVRequestStreamable
    {
        // Audio stream data
        public AudioStreamData StreamData { get; private set; }

        // Current audio clip
        public AudioClip Clip { get; private set; }
        // Ready to stream
        public bool IsStreamReady { get; private set; }
        // Ready to stream
        public bool IsStreamComplete { get; private set; }

        // Current number of samples in clip
        private int _clipMaxSamples = 0;
        // Current total samples loaded
        private int _clipSetSamples = 0;
        // Leftover byte
        private bool _hasLeftover = false;
        private byte[] _leftovers = new byte[2];
        // Current samples received
        private int _decodingChunks = 0;
        private bool _requestComplete = false;

        // Error handling
        private byte[] _errorBytes;

        // Delegate that accepts an old clip and a new clip
        public delegate void AudioStreamClipUpdateDelegate(AudioClip oldClip, AudioClip newClip);
        // Callback for audio clip update during stream
        public static event AudioStreamClipUpdateDelegate OnClipUpdated;
        // Callback for audio stream complete
        public static event Action<AudioClip> OnStreamComplete;

        // Generate
        public AudioStreamHandler(AudioStreamData streamData) : base()
        {
            // Ensure chunk size is > 0
            streamData.ClipReadyLength = Mathf.Max(0.01f, streamData.ClipReadyLength);
            int minChunkSize =
                Mathf.CeilToInt(streamData.ClipReadyLength * streamData.DecodeChannels * streamData.DecodeSampleRate);
            streamData.ClipChunkSize = Mathf.Max(minChunkSize, streamData.ClipChunkSize);

            // Apply parameters
            StreamData = streamData;

            // Setup data
            _clipMaxSamples = 0;
            _clipSetSamples = 0;
            _hasLeftover = false;
            _decodingChunks = 0;
            _requestComplete = false;
            IsStreamReady = false;
            IsStreamComplete = false;
            _errorBytes = null;

            // Generate clip immediately
            GenerateClip(StreamData.ClipChunkSize);

            // Begin stream
            VLog.D($"Clip Stream - Began\nStream Data:\n{JsonConvert.SerializeObject(streamData)}");
        }
        // If size is provided, generate clip using size
        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            // Ignore if already complete
            if (contentLength == 0 || IsStreamComplete)
            {
                return;
            }

            // Assume text if less than min chunk size
            int minChunkSize = Mathf.Max(100, Mathf.CeilToInt(StreamData.ClipReadyLength * StreamData.DecodeChannels * StreamData.DecodeSampleRate));
            if (contentLength < (ulong)minChunkSize)
            {
                _errorBytes = new byte[minChunkSize];
                return;
            }

            // Apply size
            int newMaxSamples = Mathf.Max(GetClipSamplesFromContentLength(contentLength, StreamData.DecodeType), _clipMaxSamples);
            VLog.D($"Clip Stream - Received Size\nTotal Samples: {newMaxSamples}");
            GenerateClip(newMaxSamples);
        }
        // Receive data
        protected override bool ReceiveData(byte[] receiveData, int dataLength)
        {
            // Exit if desired
            if (!base.ReceiveData(receiveData, dataLength) || IsStreamComplete)
            {
                return false;
            }

            // Append to error
            if (_errorBytes != null)
            {
                for (int i = 0; i < Mathf.Min(dataLength, _errorBytes.Length - _clipSetSamples); i++)
                {
                    _errorBytes[_clipSetSamples + i] = receiveData[i];
                }
                _clipSetSamples += dataLength;
                return true;
            }

            // Decode data async
            _decodingChunks++;
            ThreadUtility.PerformInBackground(() => DecodeData(receiveData, dataLength), OnDecodeComplete);

            // Return data
            return true;
        }
        // Decode data
        private float[] DecodeData(byte[] receiveData, int dataLength)
        {
            // Next decoded samples
            float[] newSamples = null;

            // Decode PCM chunk
            if (StreamData.DecodeType == AudioStreamDecodeType.PCM16)
            {
                newSamples = DecodeChunkPCM16(receiveData, dataLength, ref _hasLeftover, ref _leftovers);
            }

            // Failed
            return newSamples;
        }
        // Decode complete
        private void OnDecodeComplete(float[] newSamples, string error)
        {
            // Complete
            _decodingChunks--;

            // Fail with error
            if (!string.IsNullOrEmpty(error))
            {
                VLog.W($"Decode Chunk Failed\n{error}");
                TryToFinalize();
                return;
            }
            // Fail without samples
            else if (newSamples == null)
            {
                VLog.W($"Decode Chunk Failed\nNo samples returned");
                TryToFinalize();
                return;
            }

            // Generate initial clip
            if (Clip == null)
            {
                int newMaxSamples = Mathf.Max(StreamData.ClipChunkSize,
                    _clipSetSamples + newSamples.Length);
                GenerateClip(newMaxSamples);
            }
            // Generate larger clip if needed
            else if (_clipSetSamples + newSamples.Length > _clipMaxSamples)
            {
                int newMaxSamples = Mathf.Max(_clipMaxSamples + StreamData.ClipChunkSize,
                    _clipSetSamples + newSamples.Length);
                GenerateClip(newMaxSamples);
            }

            // Apply to clip
            Clip.SetData(newSamples, _clipSetSamples);
            _clipSetSamples += newSamples.Length;

            // Stream is now ready
            if (!IsStreamReady && (float)_clipSetSamples / StreamData.DecodeSampleRate >= StreamData.ClipReadyLength)
            {
                IsStreamReady = true;
                VLog.D($"Clip Stream - Stream Ready");
            }

            // Try to finalize
            TryToFinalize();
        }

        // Used for error handling
        protected override string GetText()
        {
            return _errorBytes != null ? Encoding.UTF8.GetString(_errorBytes) : string.Empty;
        }
        // Clean up clip with final sample count
        protected override void CompleteContent()
        {
            // Ignore if called multiple times
            if (_requestComplete)
            {
                return;
            }

            // Complete
            _requestComplete = true;
            TryToFinalize();
        }
        // Handle completion
        private void TryToFinalize()
        {
            // Already finalized or not yet complete
            if (IsStreamComplete || !_requestComplete || _decodingChunks > 0)
            {
                return;
            }

            // Generate final clip
            GenerateClip(_clipSetSamples);
            Clip.name = StreamData.ClipName;

            // Stream complete
            IsStreamComplete = true;
            OnStreamComplete?.Invoke(Clip);
            VLog.D($"Clip Stream - Complete\nSamples: {_clipSetSamples}");

            // Dispose
            Dispose();
        }

        // Destroy old clip
        public void CleanUp()
        {
            // Already complete
            if (IsStreamComplete)
            {
                _leftovers = null;
                _errorBytes = null;
                Clip = null;
                OnStreamComplete = null;
                return;
            }

            // Destroy clip
            if (Clip != null)
            {
                Clip.DestroySafely();
                Clip = null;
            }

            // Dispose handler
            Dispose();

            // Complete
            IsStreamComplete = true;
            VLog.D($"Clip Stream - Cleaned Up");
        }

        // Generate clip
        private void GenerateClip(int samples)
        {
            // Already generated
            if (Clip != null && _clipMaxSamples == samples)
            {
                return;
            }

            // Get old clip if applicable
            AudioClip oldClip = Clip;
            int oldClipSamples = _clipMaxSamples;

            // Generate new clip
            _clipMaxSamples = samples;
            Clip = GetCachedClip(samples, StreamData.DecodeChannels, StreamData.DecodeSampleRate);

            // If previous clip existed, get previous data
            if (oldClip != null)
            {
                // Apply existing data
                int copySamples = Mathf.Min(oldClipSamples, samples);
                float[] oldSamples = new float[copySamples];
                oldClip.GetData(oldSamples, 0);
                Clip.SetData(oldSamples, 0);

                // Invoke clip updated callback
                OnClipUpdated?.Invoke(oldClip, Clip);
                VLog.D($"Clip Stream - Clip Updated\nNew Samples: {samples}\nOld Samples: {oldClipSamples}");

                // Requeue previous clip
                ReuseCachedClip(oldClip);
            }
            else
            {
                VLog.D($"Clip Stream - Clip Generated\nSamples: {samples}");
            }
        }

        #region CACHING
        // Clip cache
        private static int _clipsGenerated = 0;
        private static List<AudioClip> _clips = new List<AudioClip>();

        /// <summary>
        /// Method used to preload clips to improve performance at runtime
        /// </summary>
        /// <param name="total">Total clips to preload.  This should be the number of clips that could be running at once</param>
        public static void PreloadCachedClips(int total, int lengthSamples, int channels, int frequency)
        {
            for (int i = 0; i < total; i++)
            {
                GenerateCacheClip(lengthSamples, channels, frequency);
            }
        }
        // Preload a single clip
        private static void GenerateCacheClip(int lengthSamples, int channels, int frequency)
        {
            _clipsGenerated++;
            AudioClip clip = AudioClip.Create($"AudioClip_{_clipsGenerated:000}", lengthSamples, channels, frequency, false);
            _clips.Add(clip);
            VLog.D($"Generating TTS Clip #{_clipsGenerated}\nSamples: {lengthSamples}");
        }
        // Preload a single clip
        private static AudioClip GetCachedClip(int lengthSamples, int channels, int frequency)
        {
            // Find a matching clip
            int clipIndex = _clips.FindIndex((clip) => DoesClipMatch(clip, lengthSamples, channels, frequency));

            // Generate a clip with the specified size
            if (clipIndex == -1)
            {
                clipIndex = _clips.Count;
                GenerateCacheClip(lengthSamples, channels, frequency);
            }

            // Get clip, remove from preload list & return
            AudioClip result = _clips[clipIndex];
            _clips.RemoveAt(clipIndex);
            return result;
        }
        // Check if clip matches
        private static bool DoesClipMatch(AudioClip clip, int lengthSamples, int channels, int frequency)
        {
            return clip.samples == lengthSamples && clip.channels == channels && clip.frequency == frequency;
        }
        // Reuse clip
        private static void ReuseCachedClip(AudioClip clip)
        {
            _clips.Add(clip);
        }
        /// <summary>
        /// Destroy all cached clips
        /// </summary>
        public static void DestroyCachedClips()
        {
            foreach (var clip in _clips)
            {
                clip.DestroySafely();
            }
            _clips.Clear();
        }
        #endregion

        #region STATIC
        // Decode raw pcm data
        public static float[] DecodeAudio(byte[] rawData, AudioStreamDecodeType decodeType)
        {
            // Samples to be decoded
            float[] samples = null;

            // Decode raw data
            if (decodeType == AudioStreamDecodeType.PCM16)
            {
                samples = DecodePCM16(rawData);
            }
            // Not supported
            else
            {
                VLog.E($"Not Supported Decode File Type\nType: {decodeType}");
            }

            // Return samples
            return samples;
        }
        // Get audio clip from samples
        private static AudioClip GetClipFromSamples(float[] samples, string clipName, int channels, int sampleRate)
        {
            AudioClip result = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }
        // Decode raw pcm data
        public static AudioClip GetClipFromRawData(byte[] rawData, AudioStreamDecodeType decodeType, string clipName, int channels, int sampleRate)
        {
            // Decode data
            float[] samples = DecodeAudio(rawData, decodeType);
            if (samples == null)
            {
                return null;
            }
            // Generate clip
            return GetClipFromSamples(samples, clipName, channels, sampleRate);
        }
        // Decode raw pcm data
        public static void GetClipFromRawDataAsync(byte[] rawData, AudioStreamDecodeType decodeType, string clipName, int channels, int sampleRate, Action<AudioClip, string> onComplete)
        {
            // Perform in background
            ThreadUtility.PerformInBackground(() => DecodeAudio(rawData, decodeType), (samples, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    error = $"Audio decode async failed\n{error}";
                    VLog.E(error);
                    onComplete?.Invoke(null, error);
                }
                else if (rawData == null)
                {
                    error = "Audio decode async results missing";
                    VLog.E(error);
                    onComplete?.Invoke(null, error);
                }
                else
                {
                    AudioClip result = GetClipFromSamples(samples, clipName, channels, sampleRate);
                    onComplete?.Invoke(result, error);
                }
            });
        }
        // Determines clip sample count via content length dependent on file type
        public static int GetClipSamplesFromContentLength(ulong contentLength, AudioStreamDecodeType decodeType)
        {
            switch (decodeType)
            {
                    case AudioStreamDecodeType.PCM16:
                        return Mathf.FloorToInt(contentLength / 2f);
            }
            return 0;
        }
        #endregion

        #region PCM DECODE
        // Decode an entire array
        public static float[] DecodePCM16(byte[] rawData)
        {
            float[] samples = new float[Mathf.FloorToInt(rawData.Length / 2f)];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = DecodeSamplePCM16(rawData, i * 2);
            }
            return samples;
        }
        // Decode a single chunk
        private static float[] DecodeChunkPCM16(byte[] chunkData, int chunkLength, ref bool hasLeftover, ref byte[] leftovers)
        {
            // Determine if previous chunk had a leftover or if newest chunk contains one
            bool prevLeftover = hasLeftover;
            bool nextLeftover = (chunkLength - (prevLeftover ? 1 : 0)) % 2 != 0;
            hasLeftover = nextLeftover;

            // Generate sample array
            int startOffset = prevLeftover ? 1 : 0;
            int endOffset = nextLeftover ? 1 : 0;
            int newSampleCount = (chunkLength + startOffset - endOffset) / 2;
            float[] newSamples = new float[newSampleCount];

            // Append first byte to previous array
            if (prevLeftover)
            {
                // Append first byte to leftover array
                leftovers[1] = chunkData[0];
                // Decode first sample
                newSamples[0] = DecodeSamplePCM16(leftovers, 0);
            }

            // Store last byte
            if (nextLeftover)
            {
                leftovers[0] = chunkData[chunkLength - 1];
            }

            // Decode remaining samples
            for (int i = 0; i < newSamples.Length - startOffset; i++)
            {
                newSamples[startOffset + i] = DecodeSamplePCM16(chunkData, startOffset + i * 2);
            }

            // Return samples
            return newSamples;
        }
        // Decode a single sample
        private static float DecodeSamplePCM16(byte[] rawData, int index)
        {
            return (float)BitConverter.ToInt16(rawData, index) / (float)Int16.MaxValue;
        }
        #endregion
    }
}
