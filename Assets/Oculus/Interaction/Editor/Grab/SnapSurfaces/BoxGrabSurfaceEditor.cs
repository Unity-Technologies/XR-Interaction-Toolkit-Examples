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
    [CustomEditor(typeof(BoxGrabSurface))]
    [CanEditMultipleObjects]
    public class BoxGrabSurfaceEditor : UnityEditor.Editor
    {
        private BoxBoundsHandle _boxHandle = new BoxBoundsHandle();
        private BoxGrabSurface _surface;
        private Transform _relativeTo;

        private SerializedProperty _relativeToProperty;

        private void OnEnable()
        {
            _boxHandle.handleColor = EditorConstants.PRIMARY_COLOR;
            _boxHandle.wireframeColor = EditorConstants.PRIMARY_COLOR_DISABLED;
            _boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z;

            _surface = (target as BoxGrabSurface);
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

            DrawRotator(_surface, _relativeTo);
            DrawBoxEditor(_surface, _relativeTo);
            DrawSlider(_surface, _relativeTo);

            if (Event.current.type == EventType.Repaint)
            {
                DrawSnapLines(_surface, _relativeTo);
            }
        }

        private void DrawSnapLines(BoxGrabSurface surface, Transform relativeTo)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            Vector3 size = surface.GetSize(relativeTo);
            Quaternion rotation = surface.GetRotation(relativeTo);
            Vector3 surfacePosition = surface.GetReferencePose(relativeTo).position;
            float widthOffset = surface.GetWidthOffset(relativeTo);
            Vector4 snapOffset = surface.GetSnapOffset(relativeTo);
            Vector3 rightAxis = rotation * Vector3.right;
            Vector3 forwardAxis = rotation * Vector3.forward;
            Vector3 forwardOffset = forwardAxis * size.z;

            Vector3 bottomLeft = surfacePosition - rightAxis * size.x * (1f - widthOffset);
            Vector3 bottomRight = surfacePosition + rightAxis * size.x * (widthOffset);
            Vector3 topLeft = bottomLeft + forwardOffset;
            Vector3 topRight = bottomRight + forwardOffset;

            Handles.DrawLine(bottomLeft + rightAxis * snapOffset.y, bottomRight + rightAxis * snapOffset.x);
            Handles.DrawLine(topLeft - rightAxis * snapOffset.x, topRight - rightAxis * snapOffset.y);
            Handles.DrawLine(bottomLeft - forwardAxis * snapOffset.z, topLeft - forwardAxis * snapOffset.w);
            Handles.DrawLine(bottomRight + forwardAxis * snapOffset.w, topRight + forwardAxis * snapOffset.z);
        }

        private void DrawSlider(BoxGrabSurface surface, Transform relativeTo)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            Vector3 size = surface.GetSize(relativeTo);
            Quaternion rotation = surface.GetRotation(relativeTo);
            Vector3 surfacePosition = surface.GetReferencePose(relativeTo).position;
            float widthOffset = surface.GetWidthOffset(relativeTo);
            Vector4 snapOffset = surface.GetSnapOffset(relativeTo);
            EditorGUI.BeginChangeCheck();
            Vector3 rightDir = rotation * Vector3.right;
            Vector3 forwardDir = rotation * Vector3.forward;
            Vector3 bottomRight = surfacePosition
                + rightDir * size.x * (widthOffset);
            Vector3 bottomLeft = surfacePosition
                - rightDir * size.x * (1f - widthOffset);
            Vector3 topRight = bottomRight + forwardDir * size.z;

            Vector3 rightHandle = DrawOffsetHandle(bottomRight + rightDir * snapOffset.x, rightDir);
            Vector3 leftHandle = DrawOffsetHandle(bottomLeft + rightDir * snapOffset.y, -rightDir);
            Vector3 topHandle = DrawOffsetHandle(topRight + forwardDir * snapOffset.z, forwardDir);
            Vector3 bottomHandle = DrawOffsetHandle(bottomRight + forwardDir * snapOffset.w, -forwardDir);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Offset Box");
                Vector4 offset = snapOffset;
                offset.x = DistanceToHandle(bottomRight, rightHandle, rightDir);
                offset.y = DistanceToHandle(bottomLeft, leftHandle, rightDir);
                offset.z = DistanceToHandle(topRight, topHandle, forwardDir);
                offset.w = DistanceToHandle(bottomRight, bottomHandle, forwardDir);
                surface.SetSnapOffset(offset, relativeTo);
            }
        }

        private Vector3 DrawOffsetHandle(Vector3 point, Vector3 dir)
        {
            float size = HandleUtility.GetHandleSize(point) * 0.2f;
            return Handles.Slider(point, dir, size, Handles.ConeHandleCap, 0f);
        }

        private float DistanceToHandle(Vector3 origin, Vector3 handlePoint, Vector3 dir)
        {
            float distance = Vector3.Distance(origin, handlePoint);
            if (Vector3.Dot(handlePoint - origin, dir) < 0f)
            {
                distance = -distance;
            }
            return distance;
        }

        private void DrawRotator(BoxGrabSurface surface, Transform relativeTo)
        {
            EditorGUI.BeginChangeCheck();
            Quaternion rotation = Handles.RotationHandle(
                surface.GetRotation(relativeTo),
                surface.GetReferencePose(relativeTo).position);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Rotation Box");
                surface.SetRotation(rotation, relativeTo);
            }
        }

        private void DrawBoxEditor(BoxGrabSurface surface, Transform relativeTo)
        {
            Quaternion rot = surface.GetRotation(relativeTo);
            Vector3 size = surface.GetSize(relativeTo);
            float widthOffset = surface.GetWidthOffset(relativeTo);
            Vector3 snapP = surface.GetReferencePose(relativeTo).position;

            _boxHandle.size = size;
            float widthPos = Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, widthOffset);
            _boxHandle.center = new Vector3(widthPos, 0f, size.z * 0.5f);

            Matrix4x4 handleMatrix = Matrix4x4.TRS(
                snapP,
                rot,
                Vector3.one
            );

            using (new Handles.DrawingScope(handleMatrix))
            {
                EditorGUI.BeginChangeCheck();
                _boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(surface, "Change Box Properties");

                    surface.SetSize(_boxHandle.size, relativeTo);
                    float width = _boxHandle.size.x;
                    if (width != 0f)
                    {
                        width = (_boxHandle.center.x + width * 0.5f) / width;
                    }
                    surface.SetWidthOffset(width, relativeTo);
                }
            }
        }
    }
}
