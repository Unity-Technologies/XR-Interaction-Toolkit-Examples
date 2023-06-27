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

using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    public class HandGrabScaleKeysEditor<TInteractable>
        where TInteractable : MonoBehaviour, IRelativeToRef
    {
        private MonoBehaviour _target;
        private SerializedObject _serializedObject;
        private List<HandGrabPose> _handGrabPoses;
        private bool _markAsOptional;
        private SerializedProperty _posesProperty;
        private ReorderableList _list;
        private HashSet<float> _scalesSet = new HashSet<float>();
        private IRelativeToRef _relativeToRef;

        private HandGhostProvider _ghostVisualsProvider;
        private HandGhost _handGhost;
        private HandPose _ghostHandPose = new HandPose();
        private Handedness _lastHandedness;

        private float _handScale = 1f;

        private const float POSE_RECT_WIDTH = 40f;
        private const float LEFT_MARGIN = 15;
        private const float RIGHT_MARGIN = 65f;
        private const float MIN_SCALE = 0.5f;
        private const float MAX_SCALE = 2f;

        public HandGrabScaleKeysEditor(SerializedObject serializedObject,
            List<HandGrabPose> handGrabPoses, string collectionName, bool markAsOptional)
        {
            _target = serializedObject.targetObject as MonoBehaviour;
            _relativeToRef = serializedObject.targetObject as IRelativeToRef;
            _posesProperty = serializedObject.FindProperty(collectionName);
            _handGrabPoses = handGrabPoses;
            _markAsOptional = markAsOptional;
            HandGhostProviderUtils.TryGetDefaultProvider(out _ghostVisualsProvider);
            _list = new ReorderableList(serializedObject, _posesProperty,
                draggable: true, displayHeader: false,
                displayAddButton: true, displayRemoveButton: true);

            InitializeListDrawers(_list);
        }

        public void TearDown()
        {
            DestroyGhost();
        }

        public void DrawInspector()
        {
            EditorGUILayout.LabelField($"{(_markAsOptional ? "[Optional]" : "")} Scaled Hand Grab Poses");

            Rect startRect = GUILayoutUtility.GetLastRect();
            _list.DoLayoutList();
            ScaledHandPoseSlider();
            CheckUniqueScales();
            CheckUniqueHandedness();
            DrawGenerationMenu();
            Rect endRect = GUILayoutUtility.GetLastRect();

            DrawBox(
                new Vector2(startRect.position.x, startRect.position.y + startRect.size.y),
                endRect.position + endRect.size);

            UpdateGhost();
        }

        private void DrawBox(Vector2 start, Vector2 end)
        {
            float margin = 5f;
            float height = end.y - start.y;
            Rect topBar = new Rect(start.x - margin, start.y,
                margin, 1f);
            Rect bottomBar = new Rect(start.x- margin, end.y,
                margin, 1f);
            Rect leftBar = new Rect(start.x - margin, start.y,
                1f, height);

            EditorGUI.DrawRect(topBar, Color.gray);
            EditorGUI.DrawRect(bottomBar, Color.gray);
            EditorGUI.DrawRect(leftBar, Color.gray);
        }

        private void InitializeListDrawers(ReorderableList list)
        {
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                SerializedProperty itemProperty = list.serializedProperty.GetArrayElementAtIndex(index);
                HandGrabPose grabPose = itemProperty.objectReferenceValue as HandGrabPose;
                float scale = float.NaN;
                if (grabPose != null)
                {
                    scale = grabPose.transform.lossyScale.x /
                        _relativeToRef.RelativeTo.lossyScale.x;
                }

                GUIContent objectLabel = new GUIContent($"Scale x{scale.ToString("F2")}");
                EditorGUI.PropertyField(rect, itemProperty, objectLabel);
            };
        }

        private void ScaledHandPoseSlider()
        {
            float minScale = MIN_SCALE;
            float maxScale = MAX_SCALE;
            for (int i = 0; i < _handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = _handGrabPoses[i];
                float scale = grabPose.transform.lossyScale.x;
                if (scale < minScale)
                {
                    minScale = scale;
                }
                if (scale > maxScale)
                {
                    maxScale = scale;
                }
            }
            _handScale = EditorGUILayout.Slider(_handScale, minScale, maxScale);
            Rect backRect = GUILayoutUtility.GetLastRect();
            backRect.x += LEFT_MARGIN;
            backRect.width -= RIGHT_MARGIN;

            for (int i = 0; i < _handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = _handGrabPoses[i];
                if (grabPose == null)
                {
                    continue;
                }
                float x = backRect.x + Mathf.InverseLerp(minScale, maxScale, grabPose.transform.lossyScale.x) * backRect.width;
                Rect poseRect = new Rect(x - POSE_RECT_WIDTH * 0.5f, backRect.y, POSE_RECT_WIDTH, backRect.height);
                EditorGUI.LabelField(poseRect, EditorGUIUtility.IconContent("curvekeyframeselected"));
            }
        }

        private void CheckUniqueScales()
        {
            _scalesSet.Clear();
            for (int i = 0; i < _handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = _handGrabPoses[i];
                if (grabPose == null)
                {
                    continue;
                }

                float scale = grabPose.transform.lossyScale.x /
                    _relativeToRef.RelativeTo.lossyScale.x;
                if (_scalesSet.Contains(scale))
                {
                    EditorGUILayout.HelpBox(
                        $"Duplicated {nameof(HandGrabPose)} of scale {scale} at index {i}.",
                        MessageType.Warning);
                }
                _scalesSet.Add(scale);
            }
        }

        private void CheckUniqueHandedness()
        {
            bool handednessSet = false;
            Handedness validHandedness = Handedness.Left;
            for (int i = 0; i < _handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = _handGrabPoses[i];
                if (grabPose == null || grabPose.HandPose == null)
                {
                    continue;
                }
                Handedness grabPoseHandedness = grabPose.HandPose.Handedness;
                if (!handednessSet)
                {
                    handednessSet = true;
                    validHandedness = grabPoseHandedness;
                }
                else if (grabPoseHandedness != validHandedness)
                {
                    EditorGUILayout.HelpBox($"Different Handedness at index {i}. " +
                        $"Ensure all HandGrabPoses have the same Handedness", MessageType.Warning);
                }
            }
        }

        private void DrawGenerationMenu()
        {
            if (GUILayout.Button($"Add HandGrabPose Key with Scale {_handScale.ToString("F2")}"))
            {
                AddHandGrabPose(_handScale);
            }

            if (GUILayout.Button("Refresh HandGrab Poses"))
            {
                RefreshHandPoses();
            }
        }

        private void AddHandGrabPose(float scale)
        {
            HandGrabPose handGrabPose = HandGrabUtils.CreateHandGrabPose(_target.transform,
                _relativeToRef.RelativeTo);
            float relativeScale = scale / _relativeToRef.RelativeTo.lossyScale.x;

            bool rangeFound = GrabPoseFinder.FindInterpolationRange(relativeScale, _handGrabPoses,
                out HandGrabPose from, out HandGrabPose to, out float t);

            handGrabPose.transform.localScale = Vector3.one * relativeScale;
            _handGrabPoses.Add(handGrabPose);
            EditorUtility.SetDirty(_target);

            if (!rangeFound)
            {
                return;
            }

            Pose relativePose = Pose.identity;
            PoseUtils.Lerp(from.RelativePose, to.RelativePose, t, ref relativePose);
            Pose rootPose = PoseUtils.GlobalPoseScaled(_relativeToRef.RelativeTo, relativePose);
            HandPose resultHandPose = new HandPose(from.HandPose);
            HandPose.Lerp(from.HandPose, to.HandPose, t, ref resultHandPose);
            Grab.GrabSurfaces.IGrabSurface surface = from.SnapSurface?.CreateDuplicatedSurface(handGrabPose.gameObject);

            handGrabPose.InjectAllHandGrabPose(_relativeToRef.RelativeTo);
            handGrabPose.InjectOptionalHandPose(resultHandPose);
            handGrabPose.InjectOptionalSurface(surface);
            handGrabPose.transform.SetPose(rootPose);
        }

        private void RefreshHandPoses()
        {
            _handGrabPoses.Clear();
            HandGrabPose[] handGrabPoses = _target.GetComponentsInChildren<HandGrabPose>();
            _handGrabPoses.AddRange(handGrabPoses);
            EditorUtility.SetDirty(_target);
        }

        #region Ghost

        private void UpdateGhost()
        {
            if (_handGrabPoses.Count == 0
                || _handGrabPoses[0].HandPose == null)
            {
                DestroyGhost();
                return;
            }

            Transform relativeTo = _relativeToRef.RelativeTo;
            Pose rootPose = Pose.identity;
            float relativeScale = _handScale / relativeTo.lossyScale.x;
            bool rangeFound = GrabPoseFinder.FindInterpolationRange(relativeScale, _handGrabPoses,
                out HandGrabPose from, out HandGrabPose to, out float t);
            if (!rangeFound)
            {
                DestroyGhost();
                return;
            }

            HandPose.Lerp(from.HandPose, to.HandPose, t, ref _ghostHandPose);
            PoseUtils.Lerp(from.RelativePose, to.RelativePose, t, ref rootPose);
            rootPose = PoseUtils.GlobalPoseScaled(relativeTo, rootPose);
            DisplayGhost(_ghostHandPose, rootPose, relativeScale);
        }

        private void DisplayGhost(HandPose handPose, Pose rootPose, float scale)
        {
            if (_handGhost != null
                && _lastHandedness != handPose.Handedness)
            {
                DestroyGhost();
            }

            _lastHandedness = handPose.Handedness;
            if (_handGhost == null)
            {
                HandGhost ghostPrototype = _ghostVisualsProvider.GetHand(_lastHandedness);
                _handGhost = GameObject.Instantiate(ghostPrototype, _target.transform);
                _handGhost.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            _handGhost.transform.localScale = Vector3.one * scale;
            _handGhost.SetPose(handPose, rootPose);
        }

        private void DestroyGhost()
        {
            if (_handGhost == null)
            {
                return;
            }
            GameObject.DestroyImmediate(_handGhost.gameObject);
            _handGhost = null;
        }
        #endregion
    }
}
