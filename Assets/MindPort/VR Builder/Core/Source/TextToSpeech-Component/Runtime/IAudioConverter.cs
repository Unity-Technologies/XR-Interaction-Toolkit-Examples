using UnityEngine;

namespace VRBuilder.TextToSpeech
{
    /// <summary>
    ///  Allows to convert audio into or out of AudioClips
    /// </summary>
    public interface IAudioConverter
    {       
        /// <summary>
        /// This method uses NAudio to convert a mp3 file given as byte array to an AudioClip in .wav format.
        /// </summary>
        /// <param name="data">Data are the bytes of an mp3 file</param>
        AudioClip CreateAudioClipFromMp3(byte[] data);

        /// <summary>
        /// This method uses NAudio to convert a wave file given as byte array to an AudioClip in .wav format.
        /// </summary>
        /// <param name="data">Data are the bytes of a wave file</param>
        AudioClip CreateAudioClipFromWAVE(byte[] data);

        /// <summary>
        /// This method uses NAudio to create .wav file from an AudioClip.
        /// </summary>
        /// <param name="filePath">Path with filename</param>
        /// <param name="audio">AudioClip to export</param>
        /// <returns>Returns if the AudioClip could wrote to an file on disk</returns>
        bool TryWriteAudioClipToFile(AudioClip audio, string filePath);
    }
}
