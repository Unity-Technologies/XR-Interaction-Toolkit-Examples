using System;
using UnityEngine;
using System.Runtime.Serialization;
using VRBuilder.Core.Audio;
using VRBuilder.Core.Attributes;
using VRBuilder.Core.Configuration;

namespace VRBuilder.TextToSpeech.Audio
{
    /// <summary>
    /// This class retrieves and stores AudioClips generated based in a provided localized text. 
    /// </summary>
    [DataContract(IsReference = true)]
    [DisplayName("Play Text to Speech")]
    public class TextToSpeechAudio : TextToSpeechContent, IAudioData
    {
        private bool isLoading;
        private string text;

        /// <inheritdoc/>
        [DataMember]
        public override string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
                if (Application.isPlaying)
                {
                    InitializeAudioClip();
                }
            }
        }

        protected TextToSpeechAudio()
        {
            text = "";
        }

        public TextToSpeechAudio(string text)
        {
            Text = text;
        }

        /// <summary>
        /// True when there is an Audio Clip loaded.
        /// </summary>
        public bool HasAudioClip
        {
            get
            {
                return AudioClip != null;
            }
        }

        /// <summary>
        /// Returns true only when is busy loading an Audio Clip.
        /// </summary>
        public bool IsLoading
        {
            get { return isLoading; }
        }

        /// <inheritdoc/>
        public AudioClip AudioClip { get; private set; }

        /// <inheritdoc/>
        public string ClipData
        {
            get
            {
                return Text;
            }
            set 
            { 
                Text = value; 
            }
        }

        public async void InitializeAudioClip()
        {
            AudioClip = null;

            if (Text == null)
            {
                Debug.LogWarning("No text provided");
                return;
            }

            if (string.IsNullOrEmpty(Text))
            {
                Debug.LogWarning($"No text provided.");
                return;
            }

            isLoading = true;
            
            try
            {
                TextToSpeechConfiguration ttsConfiguration = RuntimeConfigurator.Configuration.GetTextToSpeechConfiguration();
                ITextToSpeechProvider provider = new FileTextToSpeechProvider(ttsConfiguration);
                AudioClip = await provider.ConvertTextToSpeech(Text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception.Message);
            }
            
            isLoading = false;
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return Text == null || (string.IsNullOrEmpty(Text));
        }
    }
}
