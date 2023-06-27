using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;
using VRBuilder.TextToSpeech;

namespace VRBuilder.Editor.TextToSpeech
{
    /// <summary>
    /// This AudioConverter uses NAudio to convert audios.
    /// </summary>
    public class NAudioConverter : IAudioConverter
    {
        /// <summary>
        /// This method uses NAudio to convert a mp3 file given as byte array to an AudioClip in .wav format.
        /// </summary>
        /// <param name="data">Data are the bytes of an mp3 file</param>
        public AudioClip CreateAudioClipFromMp3(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            using (Mp3FileReader mp3Reader = new Mp3FileReader(input))
            using (WaveStream reader = WaveFormatConversionStream.CreatePcmStream(mp3Reader))
            {
                WaveFormat format = reader.WaveFormat;
                // Calculate buffer size for the AudioClip which seems half of the reader length maybe because of stereo?
                float[] buffer = new float[reader.Length / 2];
                AudioClip clip = AudioClip.Create("audio", buffer.Length, format.Channels, format.SampleRate, false);

                reader.ToSampleProvider().Read(buffer, 0, buffer.Length);
                clip.SetData(buffer, 0);
                if (clip.LoadAudioData() && clip.loadState == AudioDataLoadState.Loaded)
                {
                    return clip;
                }
            }
            throw new UnableToParseAudioFormatException("Could not parse AudioClip from given mp3 data");
        }

        public AudioClip CreateAudioClipFromWAVE(byte[] data)
        {
            using MemoryStream input = new MemoryStream(data);
            using WaveFileReader waveFileReader = new WaveFileReader(input);
            using WaveStream reader = WaveFormatConversionStream.CreatePcmStream(waveFileReader);

            WdlResamplingSampleProvider resampler = new WdlResamplingSampleProvider(reader.ToSampleProvider(), 48000);
            WaveFormat format = resampler.WaveFormat;

            // Calculate buffer size for the AudioClip which seems half of the reader length maybe because of stereo?
            float[] buffer = new float[Mathf.CeilToInt(reader.Length / 2f * (format.SampleRate / (float)reader.WaveFormat.SampleRate))];
            AudioClip clip = AudioClip.Create("audio", buffer.Length, format.Channels, format.SampleRate, false);

            resampler.Read(buffer, 0, buffer.Length);
            clip.SetData(buffer, 0);
            if (clip.LoadAudioData() && clip.loadState == AudioDataLoadState.Loaded)
            {
                return clip;
            }

            throw new UnableToParseAudioFormatException("Could not parse AudioClip from given wave data");
        }

        /// <summary>
        /// This method uses NAudio to create .wav file on disk using a given AudioClip.
        /// </summary>
        /// <param name="filePath">Path with filename</param>
        /// <param name="audio">AudioClip to export</param>
        /// <returns>Returns if the AudioClip could was written to a file on disk.</returns>
        public bool TryWriteAudioClipToFile(AudioClip audio, string filePath)
        {
            try
            {
                WaveFormat format = new WaveFormat(audio.frequency, audio.channels);
                using (WaveFileWriter writer = new WaveFileWriter(File.Create(filePath), format))
                {
                    float[] buffer = new float[audio.samples];
                    if (audio.GetData(buffer, 0))
                    {
                        writer.WriteSamples(buffer, 0, audio.samples);
                        writer.Flush();
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogErrorFormat("Could not write to disk, not authorized\n{0}", ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("Unknown exception occurred: '{0}'", ex.Message);
            }

            return false;
        }
    }
}