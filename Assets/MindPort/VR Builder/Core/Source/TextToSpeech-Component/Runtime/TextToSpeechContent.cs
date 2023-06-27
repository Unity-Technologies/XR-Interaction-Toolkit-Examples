using System.IO;
using UnityEngine;
using VRBuilder.Core.Configuration;

namespace VRBuilder.TextToSpeech.Audio
{
    /// <summary>
    /// Utility implementation of the <see cref="ITextToSpeechContent"/> interface that provides a default <see cref="IsCached"/> getter.
    /// </summary>
    public abstract class TextToSpeechContent : ITextToSpeechContent
    {
        /// <inheritdoc/>
        public abstract string Text { get; set; }

        /// <inheritdoc/>
        public bool IsCached
        {
            get
            {
                TextToSpeechConfiguration ttsConfiguration = RuntimeConfigurator.Configuration.GetTextToSpeechConfiguration();
                string filename = ttsConfiguration.GetUniqueTextToSpeechFilename(Text);
                string filePath = $"{ttsConfiguration.StreamingAssetCacheDirectoryName}/{filename}";
                return File.Exists(Path.Combine(Application.streamingAssetsPath, filePath));
            }
        }        
    }
}