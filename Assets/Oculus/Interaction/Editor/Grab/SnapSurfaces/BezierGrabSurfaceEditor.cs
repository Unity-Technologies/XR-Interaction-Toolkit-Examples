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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Grab.GrabSurfaces.Editor
{
    [CustomEditor(typeof(BezierGrabSurface))]
    [CanEditMultipleObjects]
    public class BezierGrabSurfaceEditor : UnityEditor.Editor
    {
        private BezierGrabSurface _surface;
        private SerializedProperty _relativeToProperty;
        private Transform _relativeTo;

        private bool IsSelectedIndexValid => _selectedIndex >= 0
            && _selectedIndex < _surface.ControlPoints.Count;

        private int _selectedIndex = -1;
        private const float PICK_SIZE = 0.1f;
        private const float AXIS_SIZE = 0.5f;
        private const int CURVE_STEPS = 50;
        private const float SEPARATION = 0.1f;

        private void OnEnable()
        {
            _surface = (target as BezierGrabSurface);
            _relativeToProperty = serializedObject.FindProperty("_relativeTo");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            _relativeTo = _relativeToProperty.objectReferenceValue as Transform;

            if (_relativeTo == null)
            {
                return;
            }
            if (GUILayout.Button("Add ControlPoint At Start"))
            {
                AddControlPoint(true, _relativeTo);
            }
            if (GUILayout.Button("Add ControlPoint At End"))
            {
                AddControlPoint(false, _relativeTo);
            }

            if (!IsSelectedIndexValid)
            {
                _selectedIndex = -1;
                GUILayout.Label($"No Selected Point");
            }
            else
            {
                GUILayout.Label($"Selected Point: {_selectedIndex}");
                if (GUILayout.Button("Align Selected Tangent"))
                {
                    AlignTangent(_selectedIndex, _relativeTo);
                }
                if (GUILayout.Button("Smooth Selected Tangent"))
                {
                    SmoothTangent(_selectedIndex, _relativeTo);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            if (_relativeTo == null)
            {
                return;
            }
            Handles.color = EditorConstants.PRIMARY_COLOR;

            DrawEndsCaps(_surface.ControlPoints, _relativeTo);
            if (Event.current.type == EventType.Repaint)
            {
                DrawCurve(_surface.ControlPoints, _relativeTo);
            }
        }

        private void AddControlPoint(bool addFirst, Transform relativeTo)
        {
            BezierControlPoint controlPoint = new BezierControlPoint();
            if (_surface.ControlPoints.Count == 0)
            {
                Pose pose = _surface.transform.GetPose();
                controlPoint.SetPose(pose, relativeTo);
                _surface.ControlPoints.Add(controlPoint);
                _selectedIndex = 0;
                return;
            }
            else if (_surface.ControlPoints.Count == 1)
            {
                controlPoint = _surface.ControlPoints[0];
                Pose pose = controlPoint.GetPose(relativeTo);
                pose.position += relativeTo.forward * SEPARATION;
                controlPoint.SetPose(pose, relativeTo);
            }
            else if (_surface.ControlPoints.Count > 1)
            {
                BezierControlPoint firstControlPoint;
                BezierControlPoint secondControlPoint;
                if (addFirst)
                {
                    firstControlPoint = _surface.ControlPoints[1];
                    secondControlPoint = _surface.ControlPoints[0];
                }
                else
                {
                    firstControlPoint = _surface.ControlPoints[_surface.ControlPoints.Count - 2];
                    secondControlPoint = _surface.ControlPoints[_surface.ControlPoints.Count - 1];
                }

                Pose firstPose = firstControlPoint.GetPose(relativeTo);
                Pose secondPose = secondControlPoint.GetPose(relativeTo);
                Pose controlPointPose;
                controlPointPose.position = 2 * secondPose.position - firstPose.position;
                controlPointPose.rotation = secondPose.rotation;
                controlPoint.SetPose(controlPointPose, relativeTo);
            }

            if (addFirst)
            {
                _surface.ControlPoints.Insert(0, controlPoint);
                _selectedIndex = 0;
                AlignTangent(0, relativeTo);
            }
            else
            {
                _surface.ControlPoints.Add(controlPoint);
                _selectedIndex = _surface.ControlPoints.Count - 1;
                AlignTangent(_selectedIndex - 1, relativeTo);
            }
        }

        private void AlignTangent(int index, Transform relativeTo)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            BezierControlPoint nextControlPoint = _surface.ControlPoints[(index + 1) % _surface.ControlPoints.Count];

            Vector3 tangent = (nextControlPoint.GetPose(relativeTo).position + controlPoint.GetPose(relativeTo).position) * 0.5f;
            controlPoint.SetTangent(tangent, relativeTo);
            _surface.ControlPoints[index] = controlPoint;
        }


        private void SmoothTangent(int index, Transform relativeTo)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            BezierControlPoint prevControlPoint = _surface.ControlPoints[(index + _surface.ControlPoints.Count - 1) % _surface.ControlPoints.Count];

            Vector3 tangent = prevControlPoint.GetTangent(relativeTo);
            tangent = (controlPoint.GetPose(relativeTo).position - tangent) * 0.5f;
            controlPoint.SetTangent(tangent, relativeTo);
            _surface.ControlPoints[index] = controlPoint;
        }

        private void DrawEndsCaps(List<BezierControlPoint> controlPoints, Transform relativeTo)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                DrawControlPoint(i, relativeTo);
            }

            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            if (IsSelectedIndexValid)
            {
                DrawControlPointHandles(_selectedIndex, relativeTo);
                DrawTangentLine(_selectedIndex, relativeTo);
            }
        }

        private void DrawCurve(List<BezierControlPoint> controlPoints, Transform relativeTo)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            for (int i = 0; i < controlPoints.Count && controlPoints.Count > 1; i++)
            {
                BezierControlPoint fromControlPoint = _surface.ControlPoints[i];
                Pose from = fromControlPoint.GetPose(relativeTo);

                BezierControlPoint toControlPoint = _surface.ControlPoints[(i + 1) % controlPoints.Count];
                if (toControlPoint.Disconnected)
                {
                    continue;
                }

                Pose to = toControlPoint.GetPose(relativeTo);
                Vector3 tangent = fromControlPoint.GetTangent(relativeTo);
                DrawBezier(from.position, tangent, to.position, CURVE_STEPS);
            }
        }

        private void DrawBezier(Vector3 start, Vector3 middle, Vector3 end, int steps)
        {
            Vector3 from = start;
            Vector3 to;
            float t;
            for (int i = 1; i < steps; i++)
            {
                t = i / (steps - 1f);
                to = BezierGrabSurface.EvaluateBezier(start, middle, end, t);

#if UNITY_2020_2_OR_NEWER
                Handles.DrawLine(from, to, EditorConstants.LINE_THICKNESS);
#else
                Handles.DrawLine(from, to);
#endif
                from = to;
            }
        }

        private void DrawTangentLine(int index, Transform relativeTo)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.GetPose(relativeTo);
            Vector3 center = pose.position;
            Vector3 tangent = controlPoint.GetTangent(relativeTo);

#if UNITY_2020_2_OR_NEWER
            Handles.DrawLine(center, tangent, EditorConstants.LINE_THICKNESS);
#else
            Handles.DrawLine(center, tangent);
#endif
        }

        private void DrawControlPoint(int index, Transform relativeTo)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.GetPose(relativeTo);
            float handleSize = HandleUtility.GetHandleSize(pose.position);

            Handles.color = EditorConstants.PRIMARY_COLOR;
            if (Handles.Button(pose.position, pose.rotation, handleSize * PICK_SIZE, handleSize * PICK_SIZE, Handles.DotHandleCap))
            {
                _selectedIndex = index;
            }

            Handles.color = Color.red;
            Handles.DrawLine(pose.position, pose.position + pose.right * handleSize * AXIS_SIZE);
            Handles.color = Color.green;
            Handles.DrawLine(pose.position, pose.position + pose.up * handleSize * AXIS_SIZE);
            Handles.color = Color.blue;
            Handles.DrawLine(pose.position, pose.position + pose.forward * handleSize * AXIS_SIZE);
        }

        private void DrawControlPointHandles(int index, Transform relativeTo)
        {
            BezierControlPoint controlPoint = _surface.ControlPoints[index];
            Pose pose = controlPoint.GetPose(relativeTo);
            if (Tools.current == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();
                Quaternion pointRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : pose.rotation;
                pose.position = Handles.PositionHandle(pose.position, pointRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_surface, "Change ControlPoint Position");
                    controlPoint.SetPose(pose, relativeTo);
                    _surface.ControlPoints[index] = controlPoint;
                }
            }
            else if (Tools.current == Tool.Rotate)
            {
                Quaternion originalRotation = pose.rotation;
                if (Tools.pivotRotation == PivotRotation.Global)
                {
                    Quaternion offset = Handles.RotationHandle(Quaternion.identity, pose.position);
                    pose.rotation = offset * pose.rotation;
                }
                else
                {
                    pose.rotation = Handles.RotationHandle(pose.rotation, pose.position);
                }
                pose.rotation.Normalize();
                if (originalRotation != pose.rotation)
                {
                    Undo.RecordObject(_surface, "Change ControlPoint Rotation");
                    controlPoint.SetPose(pose, relativeTo);
                    _surface.ControlPoints[index] = controlPoint;
                }
            }

            Vector3 tangent = controlPoint.GetTangent(relativeTo);
            Quaternion tangentRotation = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : pose.rotation;
            EditorGUI.BeginChangeCheck();
            tangent = Handles.PositionHandle(tangent, tangentRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_surface, "Change ControlPoint Tangent");
                controlPoint.SetTangent(tangent, relativeTo);
                _surface.ControlPoints[index] = controlPoint;
            }
        }
    }
}
