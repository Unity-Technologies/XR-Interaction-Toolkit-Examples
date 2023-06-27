using System;
using System.IO;
using SpeechLib;
using UnityEngine;
using System.Threading.Tasks;
using VRBuilder.Core.Internationalization;
using VRBuilder.TextToSpeech;

namespace VRBuilder.Editor.TextToSpeech
{
    /// <summary>
    /// TTS provider which uses Microsoft SAPI to generate audio.
    /// TextToSpeechConfig.Voice has to be either "male", "female", or "neutral".
    /// TextToSpeechConfig.Language is a language code ("de" or "de-DE" for German, "en" or "en-US" for English).
    /// It runs the TTS synthesis in a separate thread, saving the result to a temporary cache file.
    /// </summary>
    public class MicrosoftSapiTextToSpeechProvider : ITextToSpeechProvider
    {
        private TextToSpeechConfiguration configuration;

        /// <summary>
        /// This is the template of the Speech Synthesis Markup Language (SSML) string used to change the language and voice.
        /// The first argument is the preferred language code (Examples: "de" or "de-DE" for German, "en" or "en-US" for English). If the language is not installed on the system, it chooses English.
        /// The second argument is the preferred gender of the voice ("male", "female", or "neutral"). If it is not installed, it chooses another gender.
        /// The third argument is a string which is read out loud.
        /// </summary>
        private const string ssmlTemplate = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{0}'><voice languages='{0}' gender='{1}' required='languages' optional='gender'>{2}</voice></speak>";

        /// <summary>
        /// Remove the file at path and remove empty folders.
        /// </summary>
        private static void ClearCache(string path)
        {
            File.Delete(path);

            while (string.IsNullOrEmpty(path) == false)
            {
                path = Directory.GetParent(path).ToString();

                if (Directory.Exists(path) && Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
                {
                    Directory.Delete(path);
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// When the speech is generated in a separate tread, there are clicking sounds at the beginning and at the end of audio data.
        /// </summary>
        private static float[] RemoveArtifacts(float[] floats)
        {
            // Empirically determined values.
            const int elementsToRemoveFromStart = 5000;
            const int elementsToRemoveFromEnd = 10000;

            float[] cleared = new float[floats.Length - elementsToRemoveFromStart - elementsToRemoveFromEnd];

            Array.Copy(floats, elementsToRemoveFromStart, cleared, 0, floats.Length - elementsToRemoveFromStart - elementsToRemoveFromEnd);

            return cleared;
        }

        /// <summary>
        /// Set up a file stream by path.
        /// </summary>
        private static SpFileStream PrepareFileStreamToWrite(string path)
        {
            SpFileStream stream = new SpFileStream();
            SpAudioFormat format = new SpAudioFormat();
            format.Type = SpeechAudioFormatType.SAFT48kHz16BitMono;
            stream.Format = format;
            stream.Open(path, SpeechStreamFileMode.SSFMCreateForWrite, true);

            return stream;
        }

        /// <inheritdoc />
        public void SetConfig(TextToSpeechConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        /// <inheritdoc />
        public Task<AudioClip> ConvertTextToSpeech(string text)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Try to get a valid two-letter ISO language code using the provided language in the configuration.
            if (LanguageSettings.Instance.ActiveOrDefaultLanguage.TryConvertToTwoLetterIsoCode(out string twoLetterIsoCode) == false)
            {
                // If it fails, use English as default language.
                twoLetterIsoCode = "en";
                Debug.LogWarningFormat("The language \"{0}\" given in the process configuration is not valid. It was changed to default: \"en\".", LanguageSettings.Instance.ActiveOrDefaultLanguage);
            }

            // Check the validity of the voice in the configuration.
            // If it is invalid, change it to neutral.
            string voice = configuration.Voice;
            
            switch (voice.ToLower())
            {
                case "female":
                    voice = "female";
                    break;
                case "male":
                    voice = "male";
                    break;
                default:
                    voice = "neutral";
                    break;
            }
            
            string filePath = PrepareFilepathForText(text);
            float[] sampleData = Synthesize(text, filePath, twoLetterIsoCode, voice);

            AudioClip audioClip = AudioClip.Create(text, channels: 1, frequency: 48000, lengthSamples: sampleData.Length, stream: false);
            audioClip.SetData(sampleData, 0);
            
            return Task.FromResult(audioClip);
#else
            throw new PlatformNotSupportedException($"TTS audio '{text}' could not be generated due that {GetType().Name} is not supported in {Application.platform}");
#endif
        }

        private float[] Synthesize(string text, string outputPath, string language, string voice)
        {
            // Despite the fact that SpVoice.AudioOutputStream accepts values of type ISpeechBaseStream,
            // the single type of a stream that is actually working is a SpFileStream.
            SpFileStream stream = PrepareFileStreamToWrite(outputPath);
            SpVoice synthesizer = new SpVoice { AudioOutputStream = stream };
            
            string ssmlText = string.Format(ssmlTemplate, language, voice, text);
            synthesizer.Speak(ssmlText, SpeechVoiceSpeakFlags.SVSFIsXML);
            synthesizer.WaitUntilDone(-1);
            stream.Close();
            
            byte[] data = File.ReadAllBytes(outputPath);
            float[] sampleData = TextToSpeechUtils.ShortsInByteArrayToFloats(data);
            float[] cleanData = RemoveArtifacts(sampleData);
            
            ClearCache(outputPath);
            
            return cleanData;
        }

        /// <summary>
        /// Get a full path based on a <paramref name="text"/> to produce speech from, and create a directory for that.
        /// </summary>
        private string PrepareFilepathForText(string text)
        {
            string filename = configuration.GetUniqueTextToSpeechFilename(text);
            string directory = Path.Combine(Application.temporaryCachePath.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, configuration.StreamingAssetCacheDirectoryName);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, filename);
        }
    }
}
