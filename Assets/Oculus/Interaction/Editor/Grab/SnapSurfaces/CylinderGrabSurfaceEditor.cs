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
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Oculus.Interaction.Grab.GrabSurfaces.Editor
{
    [CustomEditor(typeof(CylinderGrabSurface))]
    [CanEditMultipleObjects]
    public class CylinderGrabSurfaceEditor : UnityEditor.Editor
    {
        private const float DRAW_SURFACE_ANGULAR_RESOLUTION = 5f;

        private ArcHandle _arcEndHandle = new ArcHandle();
        private ArcHandle _arcStartHandle = new ArcHandle();

        private Vector3[] _surfaceEdges;

        private CylinderGrabSurface _surface;
        private SerializedProperty _relativeToProperty;
        private Transform _relativeTo;

        private void OnEnable()
        {
            _arcStartHandle.SetColorWithRadiusHandle(EditorConstants.PRIMARY_COLOR_DISABLED, 0f);
            _arcEndHandle.SetColorWithRadiusHandle(EditorConstants.PRIMARY_COLOR, 0f);
            _surface = target as CylinderGrabSurface;
            _relativeToProperty = serializedObject.FindProperty("_relativeTo");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            _relativeTo = _relativeToProperty.objectReferenceValue as Transform;
        }

        public void OnSceneGUI()
        {
            if (_relativeTo == null)
            {
                return;
            }
            DrawEndsCaps(_surface, _relativeTo);

            float oldArcStart = _surface.ArcOffset;
            Quaternion look = Quaternion.LookRotation(_surface.GetPerpendicularDir(_relativeTo), _surface.GetDirection(_relativeTo));
            float newArcStart = DrawArcEditor(_surface, _arcStartHandle, _relativeTo,
                oldArcStart,_surface.GetStartPoint(_relativeTo),
                look);

            _surface.ArcOffset = newArcStart;
            _surface.ArcLength -= newArcStart - oldArcStart;
            _surface.ArcLength = DrawArcEditor(_surface, _arcEndHandle, _relativeTo,
                _surface.ArcLength,
                _surface.GetStartPoint(_relativeTo),
                Quaternion.LookRotation(_surface.GetStartArcDir(_relativeTo), _surface.GetDirection(_relativeTo)));

            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(_surface, _relativeTo);
            }
        }

        private void DrawEndsCaps(CylinderGrabSurface surface, Transform relativeTo)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion handleRotation = relativeTo.rotation;

            Vector3 startPosition = Handles.PositionHandle(surface.GetStartPoint(relativeTo), handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.SetStartPoint(startPosition, relativeTo);
            }
            EditorGUI.BeginChangeCheck();
            Vector3 endPosition = Handles.PositionHandle(surface.GetEndPoint(relativeTo), handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Start Cylinder Position");
                surface.SetEndPoint(endPosition, relativeTo);
            }
        }

        private void DrawSurfaceVolume(CylinderGrabSurface surface, Transform relativeTo)
        {
            Vector3 start = surface.GetStartPoint(relativeTo);
            Vector3 end = surface.GetEndPoint(relativeTo);
            Vector3 startArc = surface.GetStartArcDir(relativeTo);
            Vector3 endArc = surface.GetEndArcDir(relativeTo);
            Vector3 direction = surface.GetDirection(relativeTo);

            float radius = surface.GetRadius(relativeTo);

            Handles.color = EditorConstants.PRIMARY_COLOR;
            Handles.DrawWireArc(end,
                direction,
                startArc,
                surface.ArcLength,
                radius);

            Handles.DrawLine(start, end);
            Handles.DrawLine(start, start + startArc * radius);
            Handles.DrawLine(start, start + endArc * radius);
            Handles.DrawLine(end, end + startArc * radius);
            Handles.DrawLine(end, end + endArc * radius);

            int edgePoints = Mathf.CeilToInt((2 * surface.ArcLength) / DRAW_SURFACE_ANGULAR_RESOLUTION) + 3;
            if (_surfaceEdges == null
                || _surfaceEdges.Length != edgePoints)
            {
                _surfaceEdges = new Vector3[edgePoints];
            }

            Handles.color = EditorConstants.PRIMARY_COLOR_DISABLED;
            int i = 0;
            for (float angle = 0f; angle < surface.ArcLength; angle += DRAW_SURFACE_ANGULAR_RESOLUTION)
            {
                Vector3 dir = Quaternion.AngleAxis(angle, direction) * startArc;
                _surfaceEdges[i++] = start + dir * radius;
                _surfaceEdges[i++] = end + dir * radius;
            }
            _surfaceEdges[i++] = start + endArc * radius;
            _surfaceEdges[i++] = end + endArc * radius;
            Handles.DrawPolyLine(_surfaceEdges);
        }

        private float DrawArcEditor(CylinderGrabSurface surface, ArcHandle handle, Transform relativeTo,
            float inputAngle, Vector3 position, Quaternion rotation)
        {
            handle.radius = surface.GetRadius(relativeTo);
            handle.angle = inputAngle;

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                position,
                rotation,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                handle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Cylinder Properties");
                    return handle.angle;
                }
            }
            return inputAngle;
        }
    }
}
