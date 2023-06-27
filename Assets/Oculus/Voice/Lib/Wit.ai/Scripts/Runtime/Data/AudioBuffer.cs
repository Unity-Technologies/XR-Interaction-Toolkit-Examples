/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Lib;
using UnityEngine;

namespace Meta.WitAi.Data
{
    public class AudioBuffer : MonoBehaviour
    {
        #region Singleton
        private static AudioBuffer _instance;
        public static AudioBuffer Instance
        {
            get
            {
                if (!_instance && Application.isPlaying)
                {
                    _instance = FindObjectOfType<AudioBuffer>();
                    if (!_instance)
                    {
                        var audioBufferObject = new GameObject("AudioBuffer");
                        _instance = audioBufferObject.AddComponent<AudioBuffer>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        [SerializeField] private bool alwaysRecording;
        [SerializeField] private AudioBufferConfiguration audioBufferConfiguration = new AudioBufferConfiguration();
        [SerializeField] private AudioBufferEvents events = new AudioBufferEvents();

        public AudioBufferEvents Events => events;

        public IAudioInputSource MicInput
        {
            get
            {
                if (_micInput == null && Application.isPlaying)
                {
                    // Check this gameobject & it's children for audio input
                    _micInput = gameObject.GetComponentInChildren<IAudioInputSource>();
                    // Check all roots for Mic Input JIC
                    if (_micInput == null)
                    {
                        foreach (var root in gameObject.scene.GetRootGameObjects())
                        {
                            _micInput = root.GetComponentInChildren<IAudioInputSource>();
                            if (_micInput != null)
                            {
                                break;
                            }
                        }
                    }
                    // Use default mic script
                    if (_micInput == null)
                    {
                        _micInput = gameObject.AddComponent<Mic>();
                    }
                }

                return _micInput;
            }
        }
        private IAudioInputSource _micInput;
        private RingBuffer<byte> _micDataBuffer;

        private byte[] _byteDataBuffer;

        private HashSet<Component> _waitingRecorders = new HashSet<Component>();
        private HashSet<Component> _activeRecorders = new HashSet<Component>();

        public bool IsRecording(Component component) => _waitingRecorders.Contains(component) || _activeRecorders.Contains(component);
        public bool IsInputAvailable => MicInput != null && MicInput.IsInputAvailable;
        public void CheckForInput() => MicInput.CheckForInput();
        public AudioEncoding AudioEncoding => MicInput.AudioEncoding;

        private void Awake()
        {
            _instance = this;

            InitializeMicDataBuffer();
        }

        private void OnEnable()
        {
            MicInput.OnSampleReady += OnMicSampleReady;

            if (alwaysRecording) StartRecording(this);
        }

        // Remove mic delegates
        private void OnDisable()
        {
            MicInput.OnSampleReady -= OnMicSampleReady;

            if (alwaysRecording) StopRecording(this);
        }

        // Callback for mic sample ready
        private void OnMicSampleReady(int sampleCount, float[] sample, float levelMax)
        {
            events.OnMicLevelChanged.Invoke(levelMax);

            var marker = CreateMarker();
            Convert(sample);
            if (null != events.OnByteDataReady)
            {
                marker.Clone().ReadIntoWriters(events.OnByteDataReady.Invoke);
            }
            events.OnSampleReady?.Invoke(marker, levelMax);
        }

        // Generate mic data buffer if needed
        private void InitializeMicDataBuffer()
        {
            if (null == _micDataBuffer && audioBufferConfiguration.micBufferLengthInSeconds > 0)
            {
                var bufferSize = (int) Mathf.Ceil(2 *
                                                  audioBufferConfiguration
                                                      .micBufferLengthInSeconds * 1000 *
                                                  audioBufferConfiguration.sampleLengthInMs);
                if (bufferSize <= 0)
                {
                    bufferSize = 1024;
                }
                _micDataBuffer = new RingBuffer<byte>(bufferSize);
            }
        }

        // Convert
        private void Convert(float[] samples)
        {
            var sampleCount = samples.Length;
            const int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < sampleCount; i++)
            {
                short data = (short) (samples[i] * rescaleFactor);
                _micDataBuffer.Push((byte) data);
                _micDataBuffer.Push((byte) (data >> 8));
            }
        }

        public RingBuffer<byte>.Marker CreateMarker()
        {
            return _micDataBuffer.CreateMarker();
        }

        /// <summary>
        /// Creates a marker with an offset
        /// </summary>
        /// <param name="offset">Number of seconds to offset the marker by</param>
        /// <returns></returns>
        public RingBuffer<byte>.Marker CreateMarker(float offset)
        {
            var samples = (int) (AudioEncoding.samplerate * offset);
            return _micDataBuffer.CreateMarker(samples);
        }

        public void StartRecording(Component component)
        {
            StartCoroutine(WaitForMicToStart(component));
        }

        private IEnumerator WaitForMicToStart(Component component)
        {
            // Wait for mic
            _waitingRecorders.Add(component);
            yield return new WaitUntil(() => null != MicInput && MicInput.IsInputAvailable);
            if (!_waitingRecorders.Contains(component))
            {
                yield break;
            }
            _waitingRecorders.Remove(component);

            // Add component
            _activeRecorders.Add(component);
            // Start mic
            if (!MicInput.IsRecording)
            {
                MicInput.StartRecording(audioBufferConfiguration.sampleLengthInMs);
            }
            // On Start Listening
            if (component is IVoiceEventProvider v)
            {
                v.VoiceEvents.OnStartListening?.Invoke();
            }
        }

        public void StopRecording(Component component)
        {
            // Remove waiting recorder
            if (_waitingRecorders.Contains(component))
            {
                _waitingRecorders.Remove(component);
                return;
            }
            // Ignore unless active
            if (!_activeRecorders.Contains(component))
            {
                return;
            }

            // Remove active recorder
            _activeRecorders.Remove(component);
            // Stop recording if last active recorder
            if (_activeRecorders.Count == 0)
            {
                MicInput.StopRecording();
            }
            // On Stop Listening
            if (component is IVoiceEventProvider v)
            {
                v.VoiceEvents.OnStoppedListening?.Invoke();
            }
        }
    }
}
