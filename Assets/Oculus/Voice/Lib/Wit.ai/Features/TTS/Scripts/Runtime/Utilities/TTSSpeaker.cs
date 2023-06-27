/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.Speech;
using UnityEngine;
using UnityEngine.Serialization;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.TTS.Interfaces;

namespace Meta.WitAi.TTS.Utilities
{
    public class TTSSpeaker : MonoBehaviour, ISpeechEventProvider
    {
        #region LIFECYCLE
        // Preset voice id
        [HideInInspector] [SerializeField] public string presetVoiceID;
        public TTSVoiceSettings VoiceSettings => TTSService.GetPresetVoiceSettings(presetVoiceID);
        // Audio source
        [SerializeField] [FormerlySerializedAs("_source")]
        public AudioSource AudioSource;
        [Tooltip("Duplicates audio source reference on awake instead of using it directly.")]
        [SerializeField] private bool _cloneAudioSource = false;
        public bool CloneAudioSource => _cloneAudioSource;

        [Tooltip("Text that is added to the front of any Speech() request")]
        [TextArea]
        [SerializeField] private string prependedText;
        [TextArea]
        [Tooltip("Text that is added to the end of any Speech() text")]
        [SerializeField] private string appendedText;

        // Events
        [SerializeField] private TTSSpeakerEvents _events;
        public TTSSpeakerEvents Events => _events;
        public VoiceSpeechEvents SpeechEvents => _events;

        // Current clip to be played
        public TTSClipData SpeakingClip { get; private set; }
        // Whether currently speaking or not
        public bool IsSpeaking => SpeakingClip != null;

        // Loading clip queue
        public TTSClipData[] QueuedClips => _queuedClips.ToArray();
        // Full clip data list
        private Queue<TTSClipData> _queuedClips = new Queue<TTSClipData>();
        // Whether currently loading or not
        public bool IsLoading => _queuedClips.Count > 0;

        // Current tts service
        [SerializeField] private TTSService _ttsService;
        public TTSService TTSService
        {
            get
            {
                if (!_ttsService)
                {
                    _ttsService = GetComponent<TTSService>();
                    if (!_ttsService)
                    {
                        _ttsService = TTSService.Instance;
                    }
                }
                return _ttsService;
            }
        }

        // Check if queued
        private bool _hasQueue = false;
        private bool _willHaveQueue = false;

        // Text processors
        private ISpeakerTextPreprocessor[] _textPreprocessors;
        private ISpeakerTextPostprocessor[] _textPostprocessors;

        // Automatically generate source if needed
        protected virtual void Awake()
        {
            // Find base audio source if possible
            if (AudioSource == null)
            {
                AudioSource = gameObject.GetComponentInChildren<AudioSource>();
            }

            // Duplicate audio source
            if (CloneAudioSource)
            {
                // Create new audio source
                AudioSource instance = new GameObject($"{gameObject.name}_AudioOneShot").AddComponent<AudioSource>();
                instance.PreloadCopyData();

                // Move into this transform & default to 3D audio
                if (AudioSource == null)
                {
                    instance.transform.SetParent(transform, false);
                    instance.spread = 1f;
                }

                // Move into audio source & copy source values
                else
                {
                    instance.transform.SetParent(AudioSource.transform, false);
                    instance.Copy(AudioSource);
                }

                // Reset instance's transform
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // Apply
                AudioSource = instance;
            }

            // Setup audio source settings
            AudioSource.playOnAwake = false;

            // Get text processors
            RefreshProcessors();
        }
        // Refresh processors
        protected virtual void RefreshProcessors()
        {
            // Get preprocessors
            if (_textPreprocessors == null)
            {
                _textPreprocessors = GetComponents<ISpeakerTextPreprocessor>();
            }
            // Get postprocessors
            if (_textPostprocessors == null)
            {
                _textPostprocessors = GetComponents<ISpeakerTextPostprocessor>();
            }
            // Fix prepend text to ensure it has a space
            if (!string.IsNullOrEmpty(prependedText) && prependedText.Length > 0 && !prependedText.EndsWith(" "))
            {
                prependedText = prependedText + " ";
            }
            // Fix append text to ensure it is spaced correctly
            if (!string.IsNullOrEmpty(appendedText) && appendedText.Length > 0 && !appendedText.StartsWith(" "))
            {
                appendedText = " " + appendedText;
            }
        }
        // Stop
        protected virtual void OnDestroy()
        {
            Stop();
            _queuedClips = null;
            SpeakingClip = null;
        }
        // Add listener for clip unload
        protected virtual void OnEnable()
        {
            if (!TTSService)
            {
                return;
            }
            TTSService.Events.OnClipUnloaded.AddListener(OnClipUnload);
            TTSService.Events.Stream.OnStreamClipUpdate.AddListener(OnClipUpdated);
        }
        // Stop speaking & remove listener
        protected virtual void OnDisable()
        {
            Stop();
            if (!TTSService)
            {
                return;
            }
            TTSService.Events.OnClipUnloaded.RemoveListener(OnClipUnload);
            TTSService.Events.Stream.OnStreamClipUpdate.RemoveListener(OnClipUpdated);
        }
        // Clip unloaded externally
        protected virtual void OnClipUnload(TTSClipData clipData)
        {
            // Cancel load
            if (QueueContainsClip(clipData))
            {
                // Remove all references of the clip
                RemoveLoadingClip(clipData, true);
                // Cancel
                OnLoadCancelled(clipData);
                return;
            }
            // Cancel playback
            if (clipData.Equals(SpeakingClip))
            {
                StopSpeaking();
            }
        }
        // Clip stream complete
        protected virtual void OnClipUpdated(TTSClipData clipData)
        {
            // Ignore if not speaking clip
            if (!clipData.Equals(SpeakingClip) || AudioSource == null || !AudioSource.isPlaying)
            {
                return;
            }

            // Stop previous clip playback
            int elapsedSamples = AudioSource.timeSamples;
            AudioSource.Stop();

            // Apply new clip
            SpeakingClip = clipData;
            AudioSource.clip = SpeakingClip.clip;
            AudioSource.timeSamples = elapsedSamples;
            AudioSource.Play();
        }
        // Check queue
        private bool QueueContainsClip(TTSClipData clipData)
        {
            if (_queuedClips != null)
            {
                foreach (var clip in _queuedClips)
                {
                    if (clip.Equals(clipData))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // Refresh queue
        private void RefreshQueued()
        {
            bool newHasQueueStatus = IsLoading || IsSpeaking || _willHaveQueue;
            if (_hasQueue != newHasQueueStatus)
            {
                _hasQueue = newHasQueueStatus;
                if (_hasQueue)
                {
                    Events?.OnPlaybackQueueBegin?.Invoke();
                }
                else
                {
                    Events?.OnPlaybackQueueComplete?.Invoke();
                }
            }
        }
        #endregion

        #region TEXT
        /// <summary>
        /// Gets final text following prepending/appending & any special formatting
        /// </summary>
        /// <param name="textToSpeak">The base text to be spoken</param>
        /// <returns>Returns an array of split texts to be spoken</returns>
        public virtual string[] GetFinalText(string textToSpeak)
        {
            // Get processors
            RefreshProcessors();

            // Get results
            List<string> phrases = new List<string>();
            phrases.Add(textToSpeak);

            // Pre-processor
            if (_textPreprocessors != null)
            {
                foreach (var preprocessor in _textPreprocessors)
                {
                    preprocessor.OnPreprocessTTS(this, phrases);
                }
            }

            // Add prepend & appended text to each item
            for (int i = 0; i < phrases.Count; i++)
            {
                string phrase = phrases[i];
                phrase = $"{prependedText}{phrase}{appendedText}".Trim();
                phrases[i] = phrase;
            }

            // Post-processors
            if (_textPostprocessors != null)
            {
                foreach (var postprocessor in _textPostprocessors)
                {
                    postprocessor.OnPostprocessTTS(this, phrases);
                }
            }

            // Return all text items
            return phrases.ToArray();
        }
        /// <summary>
        /// Obtain final text list from format & text list
        /// </summary>
        /// <param name="format">The format to be used</param>
        /// <param name="textsToSpeak">The array of strings to be inserted into the format</param>
        /// <returns>Returns a list of formatted texts</returns>
        public virtual string[] GetFinalTextFormatted(string format, params string[] textsToSpeak)
        {
            return GetFinalText(GetFormattedText(format, textsToSpeak));
        }
        /// <summary>
        /// Formats text using an initial format string parameter and additional text items to
        /// be inserted into the format
        /// </summary>
        /// <param name="format">The format to be used</param>
        /// <param name="textsToSpeak">The array of strings to be inserted into the format</param>
        /// <returns>A formatted text string</returns>
        public string GetFormattedText(string format, params string[] textsToSpeak)
        {
            if (textsToSpeak != null && !string.IsNullOrEmpty(format))
            {
                object[] objects = new object[textsToSpeak.Length];
                textsToSpeak.CopyTo(objects, 0);
                return string.Format(format, objects);
            }
            return null;
        }
        #endregion

        #region REQUESTS
        /// <summary>
        /// Load a tts clip using the specified text & cache settings.
        /// Plays clip immediately upon load & will cancel all previously loading/spoken phrases.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) => Speak(textToSpeak, diskCacheSettings, false);
        public void Speak(string textToSpeak) => Speak(textToSpeak, null);
        /// <summary>
        /// Load a tts clip using the specified text & cache settings.
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void SpeakQueued(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) => Speak(textToSpeak, diskCacheSettings, true);
        public void SpeakQueued(string textToSpeak) => SpeakQueued(textToSpeak, null);

        /// <summary>
        /// Loads a formated phrase to be spoken
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        public void SpeakFormat(string format, params string[] textsToSpeak) =>
            Speak(GetFormattedText(format, textsToSpeak), null, false);
        /// <summary>
        /// Loads a formated phrase to be spoken
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        public void SpeakFormatQueued(string format, params string[] textsToSpeak) =>
            Speak(GetFormattedText(format, textsToSpeak), null, true);

        /// <summary>
        /// Speak and wait for load/playback completion
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            _willHaveQueue = true;
            Stop();
            _willHaveQueue = false;
            yield return SpeakQueuedAsync(new string[] {textToSpeak}, diskCacheSettings);
        }
        public IEnumerator SpeakAsync(string textToSpeak)
        {
            yield return SpeakAsync(textToSpeak, null);
        }
        /// <summary>
        /// Speak and wait for load/playback completion
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            // Speak each queued
            foreach (var textToSpeak in textsToSpeak)
            {
                SpeakQueued(textToSpeak, diskCacheSettings);
            }
            // Wait while loading/speaking
            yield return new WaitWhile(() => IsLoading || IsSpeaking);
        }
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak)
        {
            yield return SpeakQueuedAsync(textsToSpeak, null);
        }

        /// <summary>
        /// Loads a tts clip & handles playback
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="addToQueue">Whether or not this phrase should be enqueued into the speak queue</param>
        private void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, bool addToQueue)
        {
            // Ensure voice settings exist
            TTSVoiceSettings voiceSettings = VoiceSettings;
            if (voiceSettings == null)
            {
                VLog.E($"No voice found with preset id: {presetVoiceID}");
                return;
            }

            // Get final text phrases to be spoken
            string[] phrases = GetFinalText(textToSpeak);
            if (phrases == null || phrases.Length == 0)
            {
                VLog.W($"All phrases removed\nSource Phrase: {textToSpeak}");
                return;
            }

            // Cancel previous loading queue
            if (!addToQueue)
            {
                _willHaveQueue = true;
                StopLoading();
                _willHaveQueue = false;
            }

            // Iterate voices
            foreach (var phrase in phrases)
            {
                // Handle load
                HandleLoad(phrase, voiceSettings, diskCacheSettings, addToQueue);

                // Add additional to queue
                if (!addToQueue)
                {
                    addToQueue = true;
                }
            }
        }
        // Stop loading all items in the queue
        public virtual void StopLoading()
        {
            // Ignore if not loading
            if (!IsLoading)
            {
                return;
            }

            // Cancel each clip from loading
            while (_queuedClips.Count > 0)
            {
                OnLoadCancelled(_queuedClips.Dequeue());
            }

            // Refresh in queue check
            RefreshQueued();
        }
        // Stop playback if possible
        public virtual void StopSpeaking()
        {
            // Cannot stop speaking when not currently speaking
            if (!IsSpeaking)
            {
                return;
            }

            // Cancel playback
            HandlePlaybackComplete(true);
        }
        // Stops loading & speaking immediately
        public virtual void Stop()
        {
            StopLoading();
            StopSpeaking();
        }
        #endregion

        #region LOAD
        // Handles speaking depending on the state of the specified audio
        private void HandleLoad(string textToSpeak, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, bool addToQueue)
        {
            // Perform load request (Always waits a frame to ensure callbacks occur first)
            DateTime startTime = DateTime.Now;
            string clipId = TTSService.GetClipID(textToSpeak, voiceSettings);
            TTSClipData clipData = TTSService.Load(textToSpeak, clipId, voiceSettings, diskCacheSettings,
                (clipData2, error) => HandleLoadComplete(clipData2, error, addToQueue, startTime));

            // Ignore without clip
            if (clipData == null)
            {
                return;
            }

            // Enqueue
            _queuedClips.Enqueue(clipData);
            RefreshQueued();

            // Load begin
            OnLoadBegin(clipData);
        }
        // Load begin
        protected virtual void OnLoadBegin(TTSClipData clipData)
        {
            VLog.D($"Load Begin\nText: {clipData?.textToSpeak}");
            Events?.OnClipDataLoadBegin?.Invoke(clipData);
            Events?.OnClipLoadBegin?.Invoke(this, clipData?.textToSpeak);
            Events?.OnClipDataQueued?.Invoke(clipData);
        }
        // Load complete
        private void HandleLoadComplete(TTSClipData clipData, string error, bool addToQueue, DateTime startTime)
        {
            // Invalid clip, ignore
            if (!QueueContainsClip(clipData))
            {
                return;
            }

            // Check for other errors
            if (string.IsNullOrEmpty(error))
            {
                if (clipData.clip == null)
                {
                    error = "No clip returned";
                }
                else if (clipData.loadState == TTSClipLoadState.Error)
                {
                    error = "Error";
                }
                else if (clipData.loadState == TTSClipLoadState.Unloaded)
                {
                    error = WitConstants.CANCEL_ERROR;
                }
            }

            // Load failed
            if (!string.IsNullOrEmpty(error))
            {
                // Remove clip
                RemoveLoadingClip(clipData, false);

                // Cancelled
                if (string.Equals(WitConstants.CANCEL_ERROR, error))
                {
                    OnLoadCancelled(clipData);
                }
                // Failed
                else
                {
                    OnLoadFailed(clipData, error);
                }
                return;
            }

            // Load success event
            double loadDuration = (DateTime.Now - startTime).TotalMilliseconds;
            OnLoadSuccess(clipData, loadDuration);

            // Stop speaking except for this clip
            if (!addToQueue)
            {
                StopSpeaking();
            }

            // Playback ready
            HandlePlaybackReady(clipData);
        }
        // Remove first instance or all instances of clip
        private void RemoveLoadingClip(TTSClipData clipData, bool allInstances)
        {
            // If first & does not need all, dequeue clip
            if (!allInstances && _queuedClips.Peek().Equals(clipData))
            {
                _queuedClips.Dequeue();
                RefreshQueued();
                return;
            }

            // Otherwise create discard queue
            Queue<TTSClipData> discard = _queuedClips;
            _queuedClips = new Queue<TTSClipData>();

            // Iterate all items
            bool found = false;
            while (discard.Count > 0)
            {
                // Dequeue from discard
                TTSClipData check = discard.Dequeue();

                // Matching clip
                if (check.Equals(clipData))
                {
                    // First
                    if (!found)
                    {
                        found = true;
                    }
                    // Enqueue Duplicate
                    else if (!allInstances)
                    {
                        _queuedClips.Enqueue(check);
                    }
                }
                // Enqueue if check matches & not equal
                else if (check != null)
                {
                    _queuedClips.Enqueue(check);
                }
            }

            // Refresh in queue check
            RefreshQueued();
        }
        // Load cancelled
        protected virtual void OnLoadCancelled(TTSClipData clipData)
        {
            VLog.D($"Load Cancelled\nText: {clipData?.textToSpeak}");
            Events?.OnClipDataLoadAbort?.Invoke(clipData);
            Events?.OnClipLoadAbort?.Invoke(this, clipData?.textToSpeak);
        }
        // Load failed
        protected virtual void OnLoadFailed(TTSClipData clipData, string error)
        {
            VLog.E($"Load Failed\nText: {clipData?.textToSpeak}");
            Events?.OnClipDataLoadFailed?.Invoke(clipData);
            Events?.OnClipLoadFailed?.Invoke(this, clipData?.textToSpeak);
        }
        // Load success
        protected virtual void OnLoadSuccess(TTSClipData clipData, double loadDuration)
        {
            VLog.D($"Load Success\nText: {clipData?.textToSpeak}\nDuration: {loadDuration:0.00}ms");
            Events?.OnClipDataLoadSuccess?.Invoke(clipData);
            Events?.OnClipLoadSuccess?.Invoke(this, clipData?.textToSpeak);
        }
        #endregion

        #region READY
        // Playback ready
        private void HandlePlaybackReady(TTSClipData clipData)
        {
            // Invalid clip, ignore
            if (!QueueContainsClip(clipData))
            {
                return;
            }

            // Callback delegate
            OnPlaybackReady(clipData);

            // Attempt to play next in queue
            RefreshPlayback();
        }
        // Ready
        protected virtual void OnPlaybackReady(TTSClipData clipData)
        {
            VLog.D($"Playback Ready\nText: {clipData.textToSpeak}");
            Events?.OnAudioClipPlaybackReady?.Invoke(clipData.clip);
            Events?.OnClipDataPlaybackReady?.Invoke(clipData);
        }
        #endregion

        #region PLAYBACK
        // Wait for playback completion
        private Coroutine _waitForCompletion;

        /// <summary>
        /// Refreshes playback queue to play next available clip if possible
        /// </summary>
        private void RefreshPlayback()
        {
            // Ignore if currently playing or nothing in uque
            if (SpeakingClip != null ||  _queuedClips.Count == 0)
            {
                return;
            }
            // Peek next clip
            TTSClipData clipData = _queuedClips.Peek();
            if (clipData == null)
            {
                HandlePlaybackFailure(null, "TTSClipData no longer exists");
                return;
            }
            // Still preparing
            if (clipData.loadState == TTSClipLoadState.Preparing)
            {
                return;
            }
            if (clipData.loadState != TTSClipLoadState.Loaded)
            {
                HandlePlaybackFailure(clipData, $"TTSClipData is {clipData.loadState}");
                return;
            }
            // No audio source
            if (AudioSource == null)
            {
                HandlePlaybackFailure(clipData, "AudioSource not found");
                return;
            }
            // Somehow clip unloaded
            if (clipData.clip == null)
            {
                HandlePlaybackFailure(clipData, "AudioClip no longer exists");
                return;
            }

            // Dequeue & apply
            SpeakingClip = _queuedClips.Dequeue();

            // Started speaking
            AudioSource.clip = SpeakingClip.clip;
            AudioSource.timeSamples = 0;
            AudioSource.Play();

            // Callback events
            OnPlaybackBegin(SpeakingClip);

            // Wait for completion
            if (_waitForCompletion != null)
            {
                StopCoroutine(_waitForCompletion);
                _waitForCompletion = null;
            }
            _waitForCompletion = StartCoroutine(WaitForPlaybackComplete());
        }
        // Handles failure
        private void HandlePlaybackFailure(TTSClipData clipData, string error)
        {
            // Perform load completion
            HandleLoadComplete(clipData, error, false, default(DateTime));

            // Try to play next
            RefreshPlayback();
        }
        // Playback begin
        protected virtual void OnPlaybackBegin(TTSClipData clipData)
        {
            VLog.D($"Playback Begin\nText: {clipData.textToSpeak}");
            Events?.OnStartSpeaking?.Invoke(this, clipData.textToSpeak);
            Events?.OnTextPlaybackStart?.Invoke(clipData.textToSpeak);
            Events?.OnAudioClipPlaybackStart?.Invoke(clipData.clip);
            Events?.OnClipDataPlaybackStart?.Invoke(clipData);
        }
        // Wait for clip completion
        private IEnumerator WaitForPlaybackComplete()
        {
            // Use delta time to wait for completion
            float elapsedTime = 0f;
            while (!IsPlaybackComplete(elapsedTime))
            {
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
            }

            // Playback completed
            HandlePlaybackComplete(false);
        }
        // Check for playback completion
        protected virtual bool IsPlaybackComplete(float elapsedTime)
        {
            return SpeakingClip == null || SpeakingClip.clip == null || elapsedTime >= SpeakingClip.clip.length || (AudioSource != null && !AudioSource.isPlaying);
        }
        // Completed playback
        protected virtual void HandlePlaybackComplete(bool stopped)
        {
            // Old clip
            TTSClipData lastClipData = SpeakingClip;

            // Clear speaking clip
            SpeakingClip = null;

            // Stop playback handler
            if (_waitForCompletion != null)
            {
                StopCoroutine(_waitForCompletion);
                _waitForCompletion = null;
            }

            // Stop audio source playback
            if (AudioSource != null && AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }

            // Stopped
            if (stopped)
            {
                OnPlaybackCancelled(lastClipData, "Playback Stopped");
            }
            // No clip found
            else if (lastClipData == null)
            {
                OnPlaybackCancelled(null, "TTSClipData no longer exists");
            }
            // Clip unloaded
            else if (lastClipData.loadState == TTSClipLoadState.Unloaded)
            {
                OnPlaybackCancelled(lastClipData, "TTSClipData was unloaded");
            }
            // Clip destroyed
            else if (lastClipData.clip == null)
            {
                OnPlaybackCancelled(lastClipData, "AudioClip no longer exists");
            }
            // Success
            else
            {
                OnPlaybackComplete(lastClipData);
            }

            // Refresh in queue check
            RefreshQueued();

            // Attempt to play next in queue
            RefreshPlayback();
        }
        // Playback cancelled
        protected virtual void OnPlaybackCancelled(TTSClipData clipData, string reason)
        {
            VLog.D($"Playback Cancelled\nText: {clipData?.textToSpeak}\nReason: {reason}");
            Events?.OnCancelledSpeaking?.Invoke(this, clipData?.textToSpeak);
            Events?.OnTextPlaybackCancelled?.Invoke(clipData?.textToSpeak);
            Events?.OnAudioClipPlaybackCancelled?.Invoke(clipData?.clip);
            Events?.OnClipDataPlaybackCancelled?.Invoke(clipData);
        }
        // Playback success
        protected virtual void OnPlaybackComplete(TTSClipData clipData)
        {
            VLog.D($"Playback Finished\nText: {clipData?.textToSpeak}");
            Events?.OnFinishedSpeaking?.Invoke(this, clipData?.textToSpeak);
            Events?.OnTextPlaybackFinished?.Invoke(clipData?.textToSpeak);
            Events?.OnAudioClipPlaybackFinished?.Invoke(clipData?.clip);
            Events?.OnClipDataPlaybackFinished?.Invoke(clipData);
        }
        #endregion
    }
}
