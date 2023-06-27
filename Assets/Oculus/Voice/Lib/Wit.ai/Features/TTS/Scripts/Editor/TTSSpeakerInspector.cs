/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

namespace Meta.WitAi.TTS.Editor
{
    [CustomEditor(typeof(TTSSpeaker), true)]
    public class TTSSpeakerInspector : UnityEditor.Editor
    {
        // Speaker
        private TTSSpeaker _speaker;

        // Voices
        private int _voiceIndex = -1;
        private string[] _voices = null;

        // Voice text
        private const string UI_VOICE_HEADER = "Voice Settings";
        private const string UI_VOICE_KEY = "Voice Preset";

        // GUI
        public override void OnInspectorGUI()
        {
            // Get speaker
            if (_speaker == null)
            {
                _speaker = target as TTSSpeaker;
            }
            // Get voices
            if (_voices == null || (_voiceIndex >= 0 && _voiceIndex < _voices.Length && !string.Equals(_speaker.presetVoiceID, _voices[_voiceIndex])))
            {
                RefreshVoices();
            }

            // Voice select
            EditorGUILayout.LabelField(UI_VOICE_HEADER, EditorStyles.boldLabel);
            // No voices found
            if (_voices == null || _voices.Length == 0)
            {
                EditorGUILayout.TextField(UI_VOICE_KEY, _speaker.presetVoiceID);
            }
            // Voice dropdown
            else
            {
                bool updated = false;
                WitEditorUI.LayoutPopup(UI_VOICE_KEY, _voices, ref _voiceIndex, ref updated);
                if (updated)
                {
                    string newVoiceID = _voiceIndex >= 0 && _voiceIndex < _voices.Length
                        ? _voices[_voiceIndex]
                        : string.Empty;
                    _speaker.presetVoiceID = newVoiceID;
                    EditorUtility.SetDirty(_speaker);
                }
            }

            // Display default ui
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            base.OnInspectorGUI();

            // Layout TTS clip queue
            LayoutClipQueue();
        }

        // Layout clip queue
        private const string UI_CLIP_HEADER_TEXT = "Clip Queue";
        private const string UI_CLIP_SPEAKER_TEXT = "Speaker Clip:";
        private const string UI_CLIP_QUEUE_TEXT = "Loading Clips:";
        private bool _speakerFoldout = false;
        private bool _queueFoldout = false;
        private void LayoutClipQueue()
        {
            // Ignore unless playing
            if (!Application.isPlaying)
            {
                return;
            }

            // Add header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(UI_CLIP_HEADER_TEXT, EditorStyles.boldLabel);

            // Speaker Foldout
            _speakerFoldout = EditorGUILayout.Foldout(_speakerFoldout, UI_CLIP_SPEAKER_TEXT);
            if (_speakerFoldout)
            {
                EditorGUI.indentLevel++;
                if (!_speaker.IsSpeaking)
                {
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    TTSServiceInspector.DrawClipGUI(_speaker.SpeakingClip);
                }
                EditorGUI.indentLevel--;
            }

            // Queue Foldout
            TTSClipData[] QueuedClips = _speaker.QueuedClips;
            _queueFoldout = EditorGUILayout.Foldout(_queueFoldout, $"{UI_CLIP_QUEUE_TEXT} {(QueuedClips == null ? 0 : QueuedClips.Length)}");
            if (_queueFoldout)
            {
                EditorGUI.indentLevel++;
                if (QueuedClips == null || QueuedClips.Length == 0)
                {
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    for (int i = 0; i < QueuedClips.Length; i++)
                    {
                        TTSClipData clipData = QueuedClips[i];
                        bool oldFoldout = WitEditorUI.GetFoldoutValue(clipData);
                        bool newFoldout = EditorGUILayout.Foldout(oldFoldout, $"Clip[{i}]");
                        if (oldFoldout != newFoldout)
                        {
                            WitEditorUI.SetFoldoutValue(clipData, newFoldout);
                        }
                        if (newFoldout)
                        {
                            EditorGUI.indentLevel++;
                            TTSServiceInspector.DrawClipGUI(clipData);
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // Refresh voices
        private void RefreshVoices()
        {
            // Reset voice data
            _voiceIndex = -1;
            _voices = null;

            // Get settings
            TTSService tts = TTSService.Instance;
            TTSVoiceSettings[] settings = tts?.GetAllPresetVoiceSettings();
            if (settings == null)
            {
                VLog.E("No Preset Voice Settings Found!");
                return;
            }

            // Apply all settings
            _voices = new string[settings.Length];
            for (int i = 0; i < settings.Length; i++)
            {
                _voices[i] = settings[i].settingsID;
                if (string.Equals(_speaker.presetVoiceID, _voices[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    _voiceIndex = i;
                }
            }
        }
    }
}
