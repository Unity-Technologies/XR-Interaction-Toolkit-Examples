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

using Oculus.Interaction.Editor;
using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    [CustomEditor(typeof(HandGrabPose))]
    public class HandGrabPoseEditor : UnityEditor.Editor
    {
        private HandGrabPose _handGrabPose;
        private HandGhostProvider _ghostVisualsProvider;
        private HandGhost _handGhost;
        private Handedness _lastHandedness;
        private Transform _relativeTo;

        private int _editMode = 0;
        private SerializedProperty _handPoseProperty;
        private SerializedProperty _relativeToProperty;

        private const float GIZMO_SCALE = 0.005f;
        private static readonly string[] EDIT_MODES = new string[] { "Edit fingers", "Follow Surface" };

        private void Awake()
        {
            _handGrabPose = target as HandGrabPose;
        }

        private void OnEnable()
        {
            _handPoseProperty = serializedObject.FindProperty("_handPose");
            _relativeToProperty = serializedObject.FindProperty("_relativeTo");
            AssignMissingGhostProvider();
        }

        private void OnDisable()
        {
            DestroyGhost();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _relativeTo = _relativeToProperty.objectReferenceValue as Transform;

            if (_handGrabPose.UsesHandPose())
            {
                EditorGUILayout.PropertyField(_handPoseProperty);
            }

            GUIStyle boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField("Interactive Edition (Editor only)", boldStyle);
            if (_handGrabPose.UsesHandPose())
            {
                DrawGhostMenu(_handGrabPose.HandPose);
            }
            else
            {
                DestroyGhost();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGhostMenu(HandPose handPose)
        {
            HandGhostProvider provider = EditorGUILayout.ObjectField("Ghost Provider", _ghostVisualsProvider, typeof(HandGhostProvider), false) as HandGhostProvider;
            if (_handGhost == null
                || _ghostVisualsProvider != provider
                || _lastHandedness != handPose.Handedness)
            {
                RegenerateGhost(provider);
            }
            _ghostVisualsProvider = provider;
            _lastHandedness = handPose.Handedness;

            if (_handGrabPose.SnapSurface == null)
            {
                _editMode = 0;
            }
            else
            {
                _editMode = GUILayout.Toolbar(_editMode, EDIT_MODES);
            }
        }

        public void OnSceneGUI()
        {
            if (SceneView.currentDrawingSceneView == null)
            {
                return;
            }

            if (_handGhost == null)
            {
                return;
            }

            if (_editMode == 0)
            {
                GhostEditFingers();
            }
            else if (_editMode == 1)
            {
                GhostFollowSurface();
            }
        }


        #region ghost

        private void AssignMissingGhostProvider()
        {
            if (_ghostVisualsProvider != null)
            {
                return;
            }

            HandGhostProviderUtils.TryGetDefaultProvider(out _ghostVisualsProvider);
        }

        private void RegenerateGhost(HandGhostProvider provider)
        {
            DestroyGhost();
            CreateGhost();
        }

        private void CreateGhost()
        {
            if (_ghostVisualsProvider == null)
            {
                return;
            }
            Transform relativeTo = _handGrabPose.RelativeTo;
            HandGhost ghostPrototype = _ghostVisualsProvider.GetHand(_handGrabPose.HandPose.Handedness);
            _handGhost = GameObject.Instantiate(ghostPrototype, _handGrabPose.transform);
            _handGhost.gameObject.hideFlags = HideFlags.HideAndDontSave;

            Pose relativePose = _handGrabPose.RelativePose;
            Pose pose = PoseUtils.GlobalPoseScaled(relativeTo, relativePose);
            _handGhost.SetPose(_handGrabPose.HandPose, pose);
        }

        private void DestroyGhost()
        {
            if (_handGhost == null)
            {
                return;
            }

            GameObject.DestroyImmediate(_handGhost.gameObject);
        }

        private void GhostFollowSurface()
        {
            if (_handGhost == null)
            {
                return;
            }

            Pose ghostTargetPose = _handGrabPose.RelativePose;

            if (_handGrabPose.SnapSurface != null)
            {
                Vector3 mousePosition = Event.current.mousePosition;
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
                if (_handGrabPose.SnapSurface.CalculateBestPoseAtSurface(ray, out Pose poseInSurface, _relativeTo))
                {
                    ghostTargetPose = PoseUtils.DeltaScaled(_relativeTo, poseInSurface);
                }
            }

            _handGhost.SetRootPose(ghostTargetPose, _relativeTo);
            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            Handles.DrawSolidDisc(_handGhost.transform.position, _handGhost.transform.right, 0.01f);
        }

        private void GhostEditFingers()
        {
            HandPuppet puppet = _handGhost.GetComponent<HandPuppet>();
            if (puppet != null && puppet.JointMaps != null)
            {
                DrawBonesRotator(puppet.JointMaps);
            }
        }

        private void DrawBonesRotator(List<HandJointMap> bones)
        {
            bool anyChanged = false;
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
            {
                bool changed = false;
                HandJointId joint = FingersMetadata.HAND_JOINT_IDS[i];
                HandFinger finger = FingersMetadata.JOINT_TO_FINGER[(int)joint];

                if (_handGrabPose.HandPose.FingersFreedom[(int)finger] == JointFreedom.Free)
                {
                    continue;
                }

                HandJointMap jointMap = bones.Find(b => b.id == joint);
                if (jointMap == null)
                {
                    continue;
                }

                Transform transform = jointMap.transform;
                transform.localRotation = jointMap.RotationOffset * _handGrabPose.HandPose.JointRotations[i];

                float scale = GIZMO_SCALE * _handGrabPose.transform.lossyScale.x;
                Handles.color = EditorConstants.PRIMARY_COLOR;
                Quaternion entryRotation = transform.rotation;
                Quaternion rotation = Handles.Disc(entryRotation, transform.position,
                   transform.forward, scale, false, 0);
                if (rotation != entryRotation)
                {
                    changed = true;
                }

                if (FingersMetadata.HAND_JOINT_CAN_SPREAD[i])
                {
                    Handles.color = EditorConstants.SECONDARY_COLOR;
                    Quaternion curlRotation = rotation;
                    rotation = Handles.Disc(curlRotation, transform.position,
                        transform.up, scale, false, 0);
                    if (rotation != curlRotation)
                    {
                        changed = true;
                    }
                }

                if (!changed)
                {
                    continue;
                }

                transform.rotation = rotation;
                Undo.RecordObject(_handGrabPose, "Bone Rotation");
                _handGrabPose.HandPose.JointRotations[i] = jointMap.TrackedRotation;
                anyChanged = true;
            }

            if (anyChanged)
            {
                EditorUtility.SetDirty(_handGrabPose);
            }
        }
        #endregion

    }
}
