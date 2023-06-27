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

using UnityEngine;
using UnityEditor;
using Oculus.Interaction.Body.PoseDetection;
using Oculus.Interaction.Body.Input;
using System.IO;

namespace Oculus.Interaction.Body.Editor
{
    public class BodyPoseRecorder : EditorWindow
    {
        private const string POSE_DIRECTORY = "BodyPoses";

        [Tooltip("The " + nameof(IBody) + " that provides joint " +
            "data for pose creation.")]
        [SerializeField, Interface(typeof(IBody))]
        private UnityEngine.Object _body;

        [Tooltip("The captured body pose will be written into this " +
            nameof(BodyPoseData) + " asset. If this field is left " +
            "unassigned, each capture will create a new asset in the " +
            POSE_DIRECTORY + " directory.")]
        [SerializeField, Optional]
        private BodyPoseData _targetAsset;

        private SerializedObject _serializedEditor;

        private float _captureDelay = 5f;
        private float _nextCaptureTime;
        private bool _willCapture = false;
        private bool _beepOnCapture = true;
        private GUIStyle _richTextStyle;

        private IBody Body => _body as IBody;

        private float TimeUntilCapture =>
            Mathf.Max(_nextCaptureTime - Time.unscaledTime, 0);

        [MenuItem("Oculus/Interaction/Body Pose Recorder")]
        private static void ShowWindow()
        {
            BodyPoseRecorder window = GetWindow<BodyPoseRecorder>();
            window.titleContent = new GUIContent("Body Pose Recorder");
            window._serializedEditor = null;
            window.Show();

            if (Application.isPlaying)
            {
                window.Initialize();
            }
        }

        [InitializeOnEnterPlayMode]
        private static void OnPlayModeEnter()
        {
            EditorApplication.delayCall += () =>
            {
                if (HasOpenInstances<BodyPoseRecorder>())
                {
                    BodyPoseRecorder window = GetWindow<BodyPoseRecorder>();
                    window.Initialize();
                }
            };
        }

        private void Initialize()
        {
            _targetAsset = null;
            _body = FindObjectOfType<Input.Body>();
            _serializedEditor = new SerializedObject(this);
            _richTextStyle = EditorGUIUtility.GetBuiltinSkin(
                EditorGUIUtility.isProSkin ?
                EditorSkin.Scene :
                EditorSkin.Inspector).label;
            _richTextStyle.richText = true;
            _richTextStyle.wordWrap = true;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("Body Pose Recorder only works in Play mode");
                return;
            }
            if (_serializedEditor == null)
            {
                return;
            }

            HandleCapture();
            DrawUI();

            if (_willCapture)
            {
                Repaint();
            }
        }

        private void DrawUI()
        {
            GUI.enabled = !_willCapture;

            SerializedProperty bodyProp =
                _serializedEditor.FindProperty(nameof(_body));
            SerializedProperty targetAssetProp =
                _serializedEditor.FindProperty(nameof(_targetAsset));

            GUILayout.Label("<size=16>Source</size>\n" + bodyProp.tooltip, _richTextStyle);
            EditorGUILayout.PropertyField(
                bodyProp, new GUIContent("IBody", bodyProp.tooltip), true);

            GUILayout.Space(20);

            GUILayout.Label("<size=16>Target</size>\n" + targetAssetProp.tooltip, _richTextStyle);
            EditorGUILayout.PropertyField(
                targetAssetProp, new GUIContent("Target Asset", targetAssetProp.tooltip), true);

            GUILayout.Space(20);

            GUILayout.Label("<size=16>Capture Settings</size>", _richTextStyle);
            _captureDelay = EditorGUILayout.FloatField("Capture Delay (Seconds)", _captureDelay);
            _captureDelay = Mathf.Max(_captureDelay, 0);
            _beepOnCapture = EditorGUILayout.Toggle("Play Sound on Capture", _beepOnCapture);
            _serializedEditor.ApplyModifiedProperties();

            GUILayout.Space(20);
            GUI.enabled = Body != null;

            string buttonLabel = _willCapture ?
                $"Capturing in {TimeUntilCapture.ToString("#.#")} " +
                $"seconds\n(Click To Cancel)" : "Capture Body Pose";

            if (GUILayout.Button(buttonLabel, GUILayout.Height(36)))
            {
                _willCapture = !_willCapture;
                _nextCaptureTime = Time.unscaledTime + _captureDelay;
            }

            GUI.enabled = true;
        }

        private void HandleCapture()
        {
            if (!_willCapture)
            {
                return;
            }

            if (TimeUntilCapture <= 0f)
            {
                _willCapture = false;
                if (_beepOnCapture)
                {
                    EditorApplication.Beep();
                }

                BodyPoseData assetToWrite = _targetAsset;
                if (assetToWrite == null)
                {
                    assetToWrite = GeneratePoseAsset();
                }

                assetToWrite.SetBodyPose(Body);
                Debug.Log($"Captured Body Pose into " +
                    $"{AssetDatabase.GetAssetPath(assetToWrite)}");
                EditorUtility.SetDirty(assetToWrite);
                AssetDatabase.SaveAssetIfDirty(assetToWrite);
                AssetDatabase.Refresh();
            }
        }

        public BodyPoseData GeneratePoseAsset()
        {
            var poseDataAsset = ScriptableObject.CreateInstance<BodyPoseData>();
            string parentDir = Path.Combine("Assets", POSE_DIRECTORY);
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            string name = "BodyPose-" + $"{System.DateTime.Now.ToString("yyyyMMdd-HHmmss")}";
            AssetDatabase.CreateAsset(poseDataAsset, Path.Combine(parentDir, $"{name}.asset"));
            return poseDataAsset;
        }
    }
}
