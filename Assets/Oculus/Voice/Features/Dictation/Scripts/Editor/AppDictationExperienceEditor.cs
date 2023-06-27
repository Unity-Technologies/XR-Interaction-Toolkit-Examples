/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using Meta.WitAi;
using Meta.WitAi.Dictation;
using Meta.WitAi.Windows;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Dictation
{
    [CustomEditor(typeof(AppDictationExperience))]
    public class AppDictationExperienceEditor : Editor
    {
        [SerializeField] private string transcribeFile;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (EditorApplication.isPlaying)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("File Transcriber");
                GUILayout.BeginHorizontal();
                transcribeFile = EditorGUILayout.TextField(transcribeFile);
                if (GUILayout.Button("Browse", GUILayout.Width(75)))
                {
                    var pickedFile = EditorUtility.OpenFilePanel("Select File", "", "wav");
                    if (!string.IsNullOrEmpty(pickedFile))
                    {
                        transcribeFile = pickedFile;
                    }
                }

                GUILayout.EndHorizontal();
                if (File.Exists(transcribeFile) && GUILayout.Button("Transcribe"))
                {
                    var dictationService = ((AppDictationExperience)target).GetComponent<WitDictation>();
                    dictationService.TranscribeFile(transcribeFile);
                }

                GUILayout.EndVertical();
            }
        }
    }
}
