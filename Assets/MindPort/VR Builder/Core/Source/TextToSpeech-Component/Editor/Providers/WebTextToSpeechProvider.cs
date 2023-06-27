using System;
using System.Collections;
using System.Threading.Tasks;
using VRBuilder.Unity;
using UnityEngine;
using UnityEngine.Networking;
using VRBuilder.TextToSpeech;

namespace VRBuilder.Editor.TextToSpeech
{
    /// <summary>
    /// Abstract WebTextToSpeechProvider which can be used for web based provider.
    /// </summary>
    public abstract class WebTextToSpeechProvider : ITextToSpeechProvider
    {
        protected TextToSpeechConfiguration Configuration;

        protected readonly UnityWebRequest UnityWebRequest;

        protected readonly IAudioConverter AudioConverter;
        
        private AudioType audioType = AudioType.MPEG;

        /// <summary>
        /// The type of audio encoding for the downloaded audio clip.
        /// </summary>
        /// <remarks>Relevant for the Android platform.</remarks>
        protected AudioType AudioType
        {
            get => audioType;
            set => audioType = value;
        }

        protected WebTextToSpeechProvider() : this(new UnityWebRequest()) { }

        protected WebTextToSpeechProvider(UnityWebRequest unityWebRequest) : this(unityWebRequest, new NAudioConverter()) { }

        protected WebTextToSpeechProvider(UnityWebRequest unityWebRequest, IAudioConverter audioConverter)
        {
            UnityWebRequest = unityWebRequest;
            AudioConverter = audioConverter;
        }

        #region Public Interface
        /// <inheritdoc/>
        public void SetConfig(TextToSpeechConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <inheritdoc/>
        public async Task<AudioClip> ConvertTextToSpeech(string text)
        {
            TaskCompletionSource<AudioClip> taskCompletion = new TaskCompletionSource<AudioClip>();
            CoroutineDispatcher.Instance.StartCoroutine(DownloadAudio(text, taskCompletion));

            return await taskCompletion.Task;
        }
        #endregion

        #region Download handling
        /// <summary>
        /// Creates the specific url for the given voice, language and text to download the voice file.
        /// </summary>
        /// <param name="text">The text that should be converted into an audio file.</param>
        /// <returns>The full url required to receive the audio file for the given text message</returns>
        protected abstract string GetAudioFileDownloadUrl(string text);

        /// <summary>
        /// This method should asynchronous download the audio file to an AudioClip and call task OnFinish with it.
        /// You can use the ParseAudio method to convert the file (mp3) into an AudioClip.
        /// </summary>
        protected virtual IEnumerator DownloadAudio(string text, TaskCompletionSource<AudioClip> task)
        {
            using (UnityWebRequest request = CreateRequest(GetAudioFileDownloadUrl(text), text))
            {
                // Request and wait for the response.
                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER            
                if (request.result == UnityWebRequest.Result.ConnectionError && request.result == UnityWebRequest.Result.ProtocolError)
#else
                if (request.isNetworkError == false && request.isHttpError == false)
#endif
                {
                    byte[] data = request.downloadHandler.data;
            
                    if (data == null || data.Length == 0)
                    {
                        throw new DownloadFailedException($"Error while retrieving audio: '{request.error}'");
                    }
                    
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    task.SetResult(clip);
                }
                else
                {
                    throw new DownloadFailedException($"Error while fetching audio from '{request.uri}' backend, error: '{request.error}'");
                }
            }
        }

        /// <summary>
        /// Method to create the UnityWebRequest needed to get the file.
        /// If you have to add specific authorization or other header you can do it here.
        /// </summary>
        protected virtual UnityWebRequest CreateRequest(string url, string text)
        {
            string escapedText = UnityWebRequest.EscapeURL(text);
            Uri uri = new Uri(string.Format(url, escapedText));
            
            return UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
        }

        /// <summary>
        /// This method converts an mp3 file from byte to an AudioClip. If you have a different format, override this method.
        /// </summary>
        /// <remarks>The base implementation only works on Windows.</remarks>
        protected virtual AudioClip CreateAudioClip(byte[] data)
        {
            return AudioConverter.CreateAudioClipFromMp3(data);
        }
#endregion

        public class DownloadFailedException : Exception
        {
            public DownloadFailedException(string msg) : base(msg) { }
            
            public DownloadFailedException(string msg, Exception ex) : base(msg, ex) { }
        }
    }
}