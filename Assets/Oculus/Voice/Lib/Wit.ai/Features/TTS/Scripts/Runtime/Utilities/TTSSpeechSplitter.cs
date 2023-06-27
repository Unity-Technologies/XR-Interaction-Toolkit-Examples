/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Meta.WitAi.TTS.Interfaces;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.WitAi.TTS.Utilities
{
    public class TTSSpeechSplitter : MonoBehaviour, ISpeakerTextPreprocessor
    {
        [Tooltip("If text-to-speech phrase is greater than this length, it will be split.")]
        [Range(10, 250)] [FormerlySerializedAs("maxTextLength")]
        public int MaxTextLength = 250;

        // Regex for cleaning out SAML
        private Regex _cleaner = new Regex(@"\s\s+|</?s>|</?p>", RegexOptions.Compiled | RegexOptions.Multiline);
        // Regex for splitting
        private Regex _sentenceSplitter = new Regex(@"(?<=[.?,;!]\s+|<p>|<s>)", RegexOptions.Compiled);
        private Regex _wordSplitter = new Regex(@"(?=\s+)", RegexOptions.Compiled);

        /// <summary>
        /// Split each phrase larger than min text length into multiple phrases
        /// </summary>
        /// <param name="speaker">The speaker that will be used to speak the resulting text</param>
        /// <param name="phrases">The current phrase list that will be used for speech.  Can be added to or removed as needed.</param>
        public void OnPreprocessTTS(TTSSpeaker speaker, List<string> phrases)
        {
            // To be used
            StringBuilder message = new StringBuilder();

            // Split if possible
            int index = 0;
            while (index < phrases.Count)
            {
                // Cleanup phrase
                var text = _cleaner.Replace(phrases[index], " ");

                // If under/equal to max add cleaned phrase directly
                if (text.Length <= MaxTextLength)
                {
                    phrases[index] = text;
                    index++;
                    continue;
                }

                // Remove previous phrase from list
                phrases.RemoveAt(index);

                // Split text into sentences & iterate
                var sentences = _sentenceSplitter.Split(text);
                for (int s = 0; s < sentences.Length; s++)
                {
                    // Ignore if empty
                    var sentence = sentences[s];
                    if (sentence.Length == 0)
                    {
                        continue;
                    }

                    // If building message would be too long, finalize previous message
                    if (message.Length > 0 && message.Length + sentence.Length > MaxTextLength)
                    {
                        phrases.Insert(index, message.ToString().Trim());
                        message.Clear();
                        index++;
                    }

                    // If sentence fits, append to message
                    if (sentence.Length <= MaxTextLength)
                    {
                        message.Append(sentence);
                        continue;
                    }

                    // Sentence is longer than max length, split further
                    var words = _wordSplitter.Split(sentence);
                    for (int w = 0; w < words.Length; w++)
                    {
                        // Ignore if empty
                        string word = words[w];
                        if (word.Length == 0)
                        {
                            continue;
                        }

                        // If building message would be too long, finalize previous message
                        if (message.Length > 0 && message.Length + word.Length > MaxTextLength)
                        {
                            phrases.Insert(index, message.ToString().Trim());
                            message.Clear();
                            index++;
                        }

                        // Trim start for new message
                        if (message.Length == 0)
                        {
                            word = word.TrimStart();
                        }

                        // If word fits, append to message
                        if (word.Length <= MaxTextLength)
                        {
                            message.Append(word);
                            continue;
                        }

                        // Word is longer than max length: truncate, warn & add truncated word to tts
                        message.Append(word.Substring(0, MaxTextLength));
                        VLog.W($"Word is longer than MaxTextLength & will be truncated\nWord: {word}\nTruncated: {message}\nFrom Length: {word.Length}\nTo Length: {MaxTextLength}");
                        phrases.Insert(index, message.ToString());
                        message.Clear();
                        index++;
                    }
                }

                // Add remaining message
                if (message.Length > 0)
                {
                    phrases.Insert(index, message.ToString().Trim());
                    message.Clear();
                    index++;
                }
            }
        }
    }
}
