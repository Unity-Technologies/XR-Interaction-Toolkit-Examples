/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi.TTS.Data;

namespace Meta.WitAi.TTS.Utilities
{
    public interface ITTSPhraseProvider
    {
        /// <summary>
        /// The supported voice ids
        /// </summary>
        string[] GetVoiceIds();

        /// <summary>
        /// Get specific phrases per voice
        /// </summary>
        string[] GetVoicePhrases(string voiceId);
    }

    [RequireComponent(typeof(TTSSpeaker))]
    public class TTSSpeakerAutoLoader : MonoBehaviour, ITTSPhraseProvider
    {
        /// <summary>
        /// TTSSpeaker to be used
        /// </summary>
        public TTSSpeaker Speaker;
        /// <summary>
        /// Text file with phrases separated by line
        /// </summary>
        public TextAsset PhraseFile;
        /// <summary>
        /// All phrases to be loaded
        /// </summary>
        public string[] Phrases => _phrases;
        [SerializeField] private string[] _phrases;
        /// <summary>
        /// Whether LoadClips has to be called explicitly.
        /// If false, it is called on start
        /// </summary>
        public bool LoadManually = false;

        // Generated clips
        public TTSClipData[] Clips => _clips;
        private TTSClipData[] _clips;

        // Done loading
        public bool IsLoaded => _clipsLoading == 0;
        private int _clipsLoading = 0;

        // Load on start if not manual
        protected virtual void Start()
        {
            if (!LoadManually)
            {
                LoadClips();
            }
        }
        // Load all phrase clips
        public virtual void LoadClips()
        {
            // Done
            if (_clips != null)
            {
                VLog.W("Cannot autoload clips twice.");
                return;
            }

            // Set phrase list
            _phrases = GetAllPhrases();

            // Load all clips
            List<TTSClipData> list = new List<TTSClipData>();
            foreach (var phrase in _phrases)
            {
                _clipsLoading++;
                TTSClipData clip = TTSService.Instance.Load(phrase, Speaker.presetVoiceID, null, OnClipReady);
                list.Add(clip);
            }
            _clips = list.ToArray();
        }
        // Return all phrases
        public virtual string[] GetAllPhrases()
        {
            // Ensure speaker exists
            SetupSpeaker();

            // Get all phrases
            List<string> phrases = new List<string>();

            // Add phrases split from phrase file
            AddUniquePhrases(phrases, PhraseFile?.text.Split('\n'));
            // Add phrases serialized in phrase array
            AddUniquePhrases(phrases, Phrases);

            // Get final text
            string[] oldPhrases = phrases.ToArray();
            phrases.Clear();
            for (int i = 0; i < oldPhrases.Length; i++)
            {
                string[] newPhrases = Speaker.GetFinalText(oldPhrases[i]);
                if (newPhrases != null && newPhrases.Length > 0)
                {
                    phrases.AddRange(newPhrases);
                }
            }

            // Return array
            return phrases.ToArray();
        }
        // Add unique, non-null phrases
        private void AddUniquePhrases(List<string> list, string[] newPhrases)
        {
            if (newPhrases != null)
            {
                foreach (var phrase in newPhrases)
                {
                    if (!string.IsNullOrEmpty(phrase) && !list.Contains(phrase))
                    {
                        list.Add(phrase);
                    }
                }
            }
        }
        // Setup speaker
        protected virtual void SetupSpeaker()
        {
            if (!Speaker)
            {
                Speaker = gameObject.GetComponent<TTSSpeaker>();
                if (!Speaker)
                {
                    Speaker = gameObject.AddComponent<TTSSpeaker>();
                }
            }
        }
        // Clip ready callback
        protected virtual void OnClipReady(TTSClipData clipData, string error)
        {
            _clipsLoading--;
        }

        // Unload phrases
        protected virtual void OnDestroy()
        {
            UnloadClips();
        }
        // Unload all clips
        protected virtual void UnloadClips()
        {
            if (_clips == null)
            {
                return;
            }
            foreach (var clip in _clips)
            {
                TTSService.Instance?.Unload(clip);
            }
            _clips = null;
            _phrases = null;
        }

        #region ITTSVoicePhraseProvider
        /// <summary>
        /// Returns the supported voice ids (Only this speaker)
        /// </summary>
        public virtual string[] GetVoiceIds()
        {
            SetupSpeaker();
            string voiceId = Speaker?.presetVoiceID;
            if (string.IsNullOrEmpty(voiceId))
            {
                return null;
            }
            return new string[] {voiceId};
        }
        /// <summary>
        /// Returns the supported phrases per voice
        /// </summary>
        public virtual string[] GetVoicePhrases(string voiceId)
        {
            return GetAllPhrases();
        }
        #endregion ITTSVoicePhraseProvider
    }
}
