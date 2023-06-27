/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class OVRVirtualKeyboardSampleWPMPrompt : MonoBehaviour
{
    public class WritingPrompt
    {
        public struct PromptStats
        {
            public int inputStrokes;
            public int mistakes;
            public float totalSeconds;
            public float errorPercentage;
            public int wordsPerMinute;
            public int adjustedWordsPerMinute;
        }

        public enum PromptState
        {
            New,
            Incorrect,
            Partial,
            Correct
        };

        public class PromptWord
        {
            private string _inputBuffer = "";

            public PromptWord(string word)
            {
                this.Word = word;
                State = PromptState.New;
                LastUpdatedAt = 0;
                UpdateState(0);
            }

            public PromptState State { get; private set; }
            public string Word { get; }
            public float StartedAt { get; private set; }
            public float LastUpdatedAt { get; private set; }

            public void CommitText(string text, float time)
            {
                bool clearBuffer = " " == text.Substring(text.Length - 1, 1);
                _inputBuffer += text;

                UpdateState(time);

                if (clearBuffer)
                {
                    _inputBuffer = "";
                }
            }

            public void CommitBackspace(float time)
            {
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                }

                UpdateState(time);
            }

            public string ToRichText()
            {
                string richText = "";
                switch (State)
                {
                    case PromptState.New:
                        richText += $"<color=#ffffff>{Word}</color>";
                        break;
                    case PromptState.Partial:
                        richText +=
                            $"<color=#00dd00>{_inputBuffer}</color><color=#ddffdd>{Word.Substring(_inputBuffer.Length)}</color>";
                        break;
                    case PromptState.Correct:
                        richText += $"<color=#00dd00>{Word}</color>";
                        break;
                    case PromptState.Incorrect:
                        richText += $"<color=#ff0000>{Word}</color>";
                        break;
                }

                return richText + " ";
            }

            private void UpdateState(float time)
            {
                if (LastUpdatedAt == 0)
                {
                    StartedAt = time;
                }

                LastUpdatedAt = time;
                // Fresh start
                if (_inputBuffer.Length == 0)
                {
                    State = PromptState.New;
                }
                else if (_inputBuffer.Trim().Length == 0)
                {
                    State = PromptState.Partial;
                }
                else if (Word.Equals(_inputBuffer) || Word.Equals(_inputBuffer.TrimEnd()))
                {
                    State = PromptState.Correct;
                }
                // match
                else if (Word.Substring(0, Math.Min(_inputBuffer.Length, Word.Length)).Equals(_inputBuffer))
                {
                    State = PromptState.Partial;
                }
                // mismatch
                else
                {
                    State = PromptState.Incorrect;
                }

                if (State == PromptState.New)
                {
                    StartedAt = time;
                }
            }
        }

        private PromptStats _stats;
        private readonly LinkedList<PromptWord> _words;

        private char _previousFirstCharacter;
        private string _previousSwipe;

        public WritingPrompt(string text)
        {
            var splitText = Regex.Matches(text, @"([\w,.?!]+\s{0,1})");

            _words = new LinkedList<PromptWord>(splitText.Cast<Match>().Select(s => new PromptWord(s.Value)));

            CurrentPrompt = _words.First;
        }

        public PromptStats Stats => _stats;
        public bool IsComplete => CurrentPrompt == null;
        public LinkedListNode<PromptWord> CurrentPrompt { get; private set; }

        public void CommitText(string text, float time)
        {
            bool updatePreviousBuffer = CurrentPrompt.Value.State == PromptState.New;
            bool isSwipeSuggestion = (text.Length > 1 && _previousSwipe != String.Empty);
            bool isFirstSwipe = (text.Length > 1 && _previousSwipe == String.Empty);
            if (isSwipeSuggestion)
            {
                _stats.inputStrokes -= _previousSwipe.Length;
                // revert the mistake count
                var correctCount = GetCorrectCharacterCount(_previousSwipe);
                _stats.mistakes -= _previousSwipe.Length - correctCount;
            }
            else if (isFirstSwipe)
            {
                // Ignore/revert a swipes initial first signal character press
                _stats.inputStrokes--;
            }

            CurrentPrompt.Value.CommitText(text, time);
            _stats.inputStrokes += text.Length;
            switch (CurrentPrompt.Value.State)
            {
                case PromptState.Correct:
                    updatePreviousBuffer = false;
                    CurrentPrompt = CurrentPrompt.Next;
                    break;
                case PromptState.Incorrect:
                    if (text.Length > 1)
                    {
                        var correctCount = GetCorrectCharacterCount(text);
                        _stats.mistakes += text.Length - correctCount;
                        if (isFirstSwipe && _previousFirstCharacter != CurrentPrompt.Value.Word[0])
                        {
                            _stats.mistakes--; // Revert the initial swipe key press (if it was incorrect)
                        }
                    }
                    else
                    {
                        _stats.mistakes++;
                    }

                    break;
                case PromptState.New:
                    _stats.inputStrokes = 0;
                    _stats.mistakes = 0;
                    break;
            }

            if (updatePreviousBuffer)
            {
                _previousFirstCharacter = text[0];
                if (CurrentPrompt.Value.State != PromptState.Correct)
                {
                    _previousSwipe = (text.Length > 1) ? text : "";
                }
            }

            UpdateStats();
        }

        public void CommitBackspace(float time)
        {
            CurrentPrompt.Value.CommitBackspace(time);
        }

        public string ToRichText()
        {
            return _words.Aggregate("", (current, word) => current + word.ToRichText());
        }

        private int GetCorrectCharacterCount(string inputText)
        {
            var correctCount = 0;
            for (; correctCount < inputText.Length; correctCount++)
            {
                if (inputText[correctCount] != CurrentPrompt.Value.Word[correctCount])
                {
                    break;
                }
            }

            return correctCount;
        }

        private void UpdateStats()
        {
            _stats.errorPercentage = 0;
            _stats.wordsPerMinute = 0;
            _stats.totalSeconds = 0;
            if (_stats.inputStrokes == 0)
                return;
            var finishedTokens = _words.Where(s => s.State == PromptState.Correct).ToList();
            if (finishedTokens.Any())
            {
                _stats.errorPercentage = _stats.mistakes / (float)_stats.inputStrokes;
                var completedAt = finishedTokens.Last().LastUpdatedAt;
                var startedAt = finishedTokens.First().StartedAt;
                _stats.totalSeconds = completedAt - startedAt;
                var characters = finishedTokens.Sum(t => t.Word.Length);
                // Every 5 characters is considered a word
                var wpm = ((Mathf.Floor(characters / 5.0f)) / _stats.totalSeconds) * 60.0f;
                _stats.wordsPerMinute = Mathf.FloorToInt(wpm);
                _stats.adjustedWordsPerMinute = Mathf.FloorToInt(wpm * (1 - _stats.errorPercentage));
            }
        }
    }

    private const string PlayerPrefsRecordPrefix = "metaVirtualKeyboardSampleRecord_";
    private static readonly string PlayerPrefsRecordAWPM = $"{PlayerPrefsRecordPrefix}AWPM";
    private static readonly string PlayerPrefsRecordWPM = $"{PlayerPrefsRecordPrefix}WPM";
    private static readonly string PlayerPrefsRecordErrorPercentage = $"{PlayerPrefsRecordPrefix}ErrorPercentage";

    private const string CompletePromptRichText = "<i>- Complete a prompt -</i>";

    public Action<WritingPrompt.PromptStats> OnWritingPromptComplete;

    [SerializeField]
    private Text typingPrompt;

    [SerializeField]
    private Text statsOutput;

    [SerializeField]
    private Text recordOutput;

    [SerializeField]
    public OVRVirtualKeyboard VirtualKeyboard;

    private static readonly List<string> Sentences = new List<string>()
    {
        "The quick brown fox jumped over the lazy dog",
        "Bruce is the loose moose in the goose caboose",
        "The charitable endowment intoxicated the forest spirits",
        "The virtual keyboard is fantastic"
    };

    private int _currentSentenceIndex;
    private WritingPrompt _activeWritingPrompt;

    private WritingPrompt.PromptStats RecordStat
    {
        get =>
            new WritingPrompt.PromptStats
            {
                adjustedWordsPerMinute = PlayerPrefs.GetInt(PlayerPrefsRecordAWPM, 0),
                wordsPerMinute = PlayerPrefs.GetInt(PlayerPrefsRecordWPM, 0),
                errorPercentage = PlayerPrefs.GetFloat(PlayerPrefsRecordErrorPercentage, 0)
            };
        set
        {
            PlayerPrefs.SetInt(PlayerPrefsRecordAWPM, value.adjustedWordsPerMinute);
            PlayerPrefs.SetInt(PlayerPrefsRecordWPM, value.wordsPerMinute);
            PlayerPrefs.SetFloat(PlayerPrefsRecordErrorPercentage, value.errorPercentage);
        }
    }

    private void Start()
    {
        CycleWritingPrompt();
        UpdateTypingPrompt();

        VirtualKeyboard.CommitText += OnCommitText;
        VirtualKeyboard.Backspace += OnBackspace;
        VirtualKeyboard.Enter += OnEnter;
    }

    private void OnDestroy()
    {
        VirtualKeyboard.CommitText -= OnCommitText;
        VirtualKeyboard.Backspace -= OnBackspace;
        VirtualKeyboard.Enter -= OnEnter;
    }

    private void UpdateTypingPrompt()
    {
        typingPrompt.text = _activeWritingPrompt.ToRichText();
    }

    private void OnEnter()
    {
        CycleWritingPrompt();
        UpdateTypingPrompt();
    }

    private void OnBackspace()
    {
        _activeWritingPrompt.CommitBackspace(Time.time);
        UpdateTypingPrompt();
    }

    private void OnCommitText(string newText)
    {
        if (newText[0] == ' ')
        {
            newText = " ";
        }
        else
        {
            newText = newText.Trim();
            if (newText.Length > 1)
            {
                newText += " ";
            }
        }

        _activeWritingPrompt.CommitText(newText, Time.time);


        if (_activeWritingPrompt.IsComplete)
        {
            CycleWritingPrompt();
        }

        UpdateTypingPrompt();
    }

    private string StatToText(WritingPrompt.PromptStats stats)
    {
        return $"WPM: {stats.wordsPerMinute:0.#}\n"
               + $"Accuracy: {(1.0f - stats.errorPercentage) * 100.0f:0.#\\%}\n"
               + $"AWPM: {stats.adjustedWordsPerMinute}";
    }

    private void CycleWritingPrompt()
    {
        WritingPrompt.PromptStats recordStat = RecordStat;
        if (_activeWritingPrompt != null)
        {
            var activeStat = _activeWritingPrompt.Stats;
            statsOutput.text = StatToText(activeStat);
            if (_activeWritingPrompt.IsComplete)
            {
                // Check for new record
                if (activeStat.adjustedWordsPerMinute > recordStat.adjustedWordsPerMinute)
                {
                    recordStat.adjustedWordsPerMinute = activeStat.adjustedWordsPerMinute;
                    recordStat.wordsPerMinute = activeStat.wordsPerMinute;
                    recordStat.errorPercentage = activeStat.errorPercentage;
                    RecordStat = recordStat;
                }

                // cycle to new sentence
                _currentSentenceIndex++;
                if (_currentSentenceIndex >= Sentences.Count)
                {
                    _currentSentenceIndex = 0;
                }

                OnWritingPromptComplete?.Invoke(activeStat);
            }
        }
        else
        {
            statsOutput.text = CompletePromptRichText;
        }

        recordOutput.text = recordStat.wordsPerMinute != 0 ? StatToText(recordStat) : CompletePromptRichText;

        _activeWritingPrompt = new WritingPrompt(Sentences[_currentSentenceIndex]);
    }
}
