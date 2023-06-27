using System;
using VRBuilder.Core.Configuration;

namespace VRBuilder.TextToSpeech
{
    /// <summary>
    /// TextToSpeech extensions methods for <see cref="IRuntimeConfiguration"/>.
    /// </summary>
    [Obsolete("This class is obsolete and replaced by RuntimeConfigurationExtensions.")]
    public static class IRuntimeConfigurationExtensions
    {
        /// <summary>
        /// Text to speech configuration.
        /// </summary>
        private static TextToSpeechConfiguration textToSpeechConfiguration;
        
        /// <summary>
        /// Return loaded <see cref="TextToSpeechConfiguration"/>.
        /// </summary>
        [Obsolete("To be more flexible with development we switched to an abstract class as configuration base, consider using BaseRuntimeConfiguration.")]
        public static TextToSpeechConfiguration GetTextToSpeechConfiguration(this IRuntimeConfiguration runtimeConfiguration)
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
        [Obsolete("To be more flexible with development we switched to an abstract class as configuration base, consider using BaseRuntimeConfiguration.")]
        public static void SetTextToSpeechConfiguration(this IRuntimeConfiguration runtimeConfiguration, TextToSpeechConfiguration ttsConfiguration)
        {
            textToSpeechConfiguration = ttsConfiguration;
        }
    }
}
