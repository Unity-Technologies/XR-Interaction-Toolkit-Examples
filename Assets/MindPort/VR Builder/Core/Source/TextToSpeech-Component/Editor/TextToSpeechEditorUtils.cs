using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using VRBuilder.Core;
using VRBuilder.Core.Configuration;
using VRBuilder.TextToSpeech;
using VRBuilder.TextToSpeech.Audio;

namespace VRBuilder.Editor.TextToSpeech
{
    public static class TextToSpeechEditorUtils
    {
        /// <summary>
        /// Generates TTS audio and creates a file.
        /// </summary>       
        public static async Task CacheAudioClip(string text, TextToSpeechConfiguration configuration)
        {
            string filename = configuration.GetUniqueTextToSpeechFilename(text);
            string filePath = $"{configuration.StreamingAssetCacheDirectoryName}/{filename}";

            ITextToSpeechProvider provider = TextToSpeechProviderFactory.Instance.CreateProvider(configuration);
            AudioClip audioClip = await provider.ConvertTextToSpeech(text);

            CacheAudio(audioClip, filePath, new NAudioConverter());
        }

        /// <summary>
        /// Stores given <paramref name="audioClip"/> in a cached directory.
        /// </summary>
        /// <remarks>When used in the Unity Editor the cached directory is inside the StreamingAssets folder; Otherwise during runtime the base path is the platform
        /// persistent data.</remarks>
        /// <param name="audioClip">The audio file to be cached.</param>
        /// <param name="filePath">Relative path where the <paramref name="audioClip"/> will be stored.</param>
        /// <returns>True if the file was successfully cached.</returns>
        private static bool CacheAudio(AudioClip audioClip, string filePath, IAudioConverter converter)
        {
            // Ensure target directory exists.
            string fileName = Path.GetFileName(filePath);
            string relativePath = Path.GetDirectoryName(filePath);

            string basedDirectoryPath = Application.isEditor ? Application.streamingAssetsPath : Application.persistentDataPath;
            string absolutePath = Path.Combine(basedDirectoryPath, relativePath);

            if (string.IsNullOrEmpty(absolutePath) == false && Directory.Exists(absolutePath) == false)
            {
                Directory.CreateDirectory(absolutePath);
            }

            string absoluteFilePath = Path.Combine(absolutePath, fileName);

            return converter.TryWriteAudioClipToFile(audioClip, absoluteFilePath);
        }

        /// <summary>
        /// Generates files for all <see cref="TextToSpeechAudio"/> passed.
        /// </summary>        
        public static async Task<int> CacheTextToSpeechClips(IEnumerable<ITextToSpeechContent> clips)
        {
            TextToSpeechConfiguration configuration = RuntimeConfigurator.Configuration.GetTextToSpeechConfiguration();

            ITextToSpeechContent[] validClips = clips.Where(clip => string.IsNullOrEmpty(clip.Text) == false).ToArray();

            for(int i = 0; i < validClips.Length; i++ )
            {
                EditorUtility.DisplayProgressBar($"Generating audio with {configuration.Provider}", $"{i + 1}/{validClips.Length}: {validClips[i].Text}", (float)i / validClips.Length);
                await CacheAudioClip(validClips[i].Text, configuration);
            }

            EditorUtility.ClearProgressBar();
            return validClips.Length;
        }

        public static async void GenerateTextToSpeechForAllProcesses()
        {
            IEnumerable<string> processNames = ProcessAssetUtils.GetAllProcesses();
            bool filesGenerated = false;

            foreach (string processName in processNames)
            {
                IProcess process = ProcessAssetManager.Load(processName);

                if (process != null)
                {
                    IEnumerable<ITextToSpeechContent> tts = EditorReflectionUtils.GetNestedPropertiesFromData<ITextToSpeechContent>(process.Data).Where(content => content.IsCached == false && string.IsNullOrEmpty(content.Text) == false);

                    if (tts.Count() > 0)
                    {
                        filesGenerated = true;
                        int clips = await CacheTextToSpeechClips(tts);
                        Debug.Log($"Generated {clips} audio files for process '{process.Data.Name}.'");
                    }
                }
            }

            if(filesGenerated == false)
            {
                Debug.Log("Found no TTS content to generate.");
            }
        }
    }
}