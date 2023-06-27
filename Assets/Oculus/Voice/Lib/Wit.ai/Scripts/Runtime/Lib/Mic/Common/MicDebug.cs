/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using Meta.WitAi;
using Meta.WitAi.Interfaces;
using UnityEngine;

namespace Meta.WitAi.Lib
{
    public class MicDebug : MonoBehaviour
    {
        // References & Settings
        [SerializeField] private IAudioInputSource _micSource;
        [SerializeField] private string fileDirectory = "MicClips";
        [SerializeField] private string fileName = "mic_debug_";

        // Fields for writing
        private FileStream _fileStream;
        private byte[] _buffer;

        // Add delegates
        private void OnEnable()
        {
            if (_micSource == null)
            {
                _micSource = gameObject.GetComponentInChildren<IAudioInputSource>();
            }
            if (_micSource != null)
            {
                _micSource.OnStartRecording += OnStartRecording;
                _micSource.OnSampleReady += OnSampleReady;
                _micSource.OnStopRecording += OnStopRecording;
            }
        }
        // Remove delegates
        private void OnDisable()
        {
            if (_micSource != null)
            {
                _micSource.OnStartRecording -= OnStartRecording;
                _micSource.OnSampleReady -= OnSampleReady;
                _micSource.OnStopRecording -= OnStopRecording;
            }
        }
        // Unload on destroy
        private void OnDestroy()
        {
            UnloadStream();
        }

        // Start recording
        private void OnStartRecording()
        {
            // Get root
            #if UNITY_EDITOR
            string path = Application.dataPath.Replace("\\", "/").Replace("/Assets", "");
            #else
            string path = Application.temporaryCachePath;
            #endif

            // Add & create directory
            path += "/" + fileDirectory;
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Generate file name
            DateTime now = DateTime.Now;
            path = $"{path}/{fileName}{now.Year:0000}{now.Month:00}{now.Day:00}_{now.Hour:00}{now.Minute:00}{now.Second:00}.pcm";
            VLog.D("MicDebug - Writing recording to file: " + path);

            // Create file stream
            _fileStream = File.Open(path, FileMode.Create);
        }
        // Sample ready
        private const int FLOAT_TO_SHORT = 0b111111111111111; //dec: 32767
        private const int BYTES_PER_SHORT = 2;
        private void OnSampleReady(int sampleCount, float[] sample, float levelMax)
        {
            // Ignore without stream
            if (_fileStream == null || sample == null)
            {
                return;
            }

            // Recreate buffer
            if (_buffer == null || _buffer.Length != sample.Length * BYTES_PER_SHORT)
            {
                _buffer = new byte[sample.Length * BYTES_PER_SHORT];
            }

            // Convert float to Int16
            for (int i = 0; i < sample.Length; i++)
            {
                short data = (short) (sample[i] * FLOAT_TO_SHORT);
                _buffer[i * BYTES_PER_SHORT] = (byte) data;
                _buffer[i * BYTES_PER_SHORT + 1] = (byte) (data >> 8);
            }

            // Write data
            _fileStream.Write(_buffer, 0, _buffer.Length);
        }
        // Unload immediately
        private void OnStopRecording()
        {
            UnloadStream();
        }
        // Cleanup write stream
        private void UnloadStream()
        {
            if (_fileStream == null)
            {
                return;
            }
            _fileStream.Close();
            _fileStream.Dispose();
            _fileStream = null;
        }
    }
}
