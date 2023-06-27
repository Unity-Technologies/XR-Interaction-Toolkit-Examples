using VRBuilder.Core.Configuration;

namespace VRBuilder.TextToSpeech
{
    /// <summary>
    /// TextToSpeech extensions methods for <see cref="BaseRuntimeConfiguration"/>.
    /// </summary>
    public static class RuntimeConfigurationExtensions
    {
        /// <summary>
        /// Text to speech configuration.
        /// </summary>
        private static TextToSpeechConfiguration textToSpeechConfiguration;

        /// <summary>
        /// Return loaded <see cref="TextToSpeechConfiguration"/>.
        /// </summary>
        public static TextToSpeechConfiguration GetTextToSpeechConfiguration(this BaseRuntimeConfiguration runtimeConfiguration)
        {
            if (textToSpeechConfiguration == null)
            {
                textToSpeechConfiguration = TextToSpeechConfiguration.LoadConfiguration();
            }

            return textToSpeechConfiguration;
        }

        /// <summary>
        /// Loads a new <see cref="TextToSpeechConfiguration"/>
        /// </summary>
        public static void SetTextToSpeechConfiguration(this BaseRuntimeConfiguration runtimeConfiguration,
            TextToSpeechConfiguration ttsConfiguration)
        {
            textToSpeechConfiguration = ttsConfiguration;
        }
    }
}