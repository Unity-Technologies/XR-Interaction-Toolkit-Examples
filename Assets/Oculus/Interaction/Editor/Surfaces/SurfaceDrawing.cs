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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction.Editor
{
    public static class SurfaceDrawing
    {
        public static void DrawCylinderSegment(Cylinder cylinder, CylinderSegment segment)
        {
            const int SEGMENTS_PER_UNIT = 5;

            if (cylinder == null ||
                segment.ArcDegrees <= 0f)
            {
                return;
            }

            // Handle infinite height using scene camera Y
            float top, bottom;
            if (segment.IsInfiniteHeight)
            {
                if (SceneView.lastActiveSceneView != null &&
                    SceneView.lastActiveSceneView.camera != null)
                {
                    Vector3 cameraPos =
                        cylinder.transform.InverseTransformPoint(
                            SceneView.lastActiveSceneView.camera.transform.position);
                    bottom = cameraPos.y - 10;
                    top = cameraPos.y + 10;
                }
                else
                {
                    bottom = -30;
                    top = 30;
                }
            }
            else
            {
                bottom = segment.Bottom;
                top = segment.Top;
            }

            float height = top - bottom;
            float width = segment.ArcDegrees * Mathf.Deg2Rad * cylinder.Radius;
            int verticalSegments = Mathf.Max(2, Mathf.CeilToInt(SEGMENTS_PER_UNIT * height));
            int horizontalSegments = Mathf.Max(2, Mathf.FloorToInt(SEGMENTS_PER_UNIT * width));

            for (int v = 0; v <= verticalSegments; ++v)
            {
                float y = Mathf.Lerp(bottom, top, (float)v / verticalSegments);
                DrawArc(cylinder, segment, y);
            }

            for (int h = 0; h <= horizontalSegments; ++h)
            {
                float x = Mathf.Lerp(-segment.ArcDegrees / 2,
                                     segment.ArcDegrees / 2,
                                     (float)h / horizontalSegments);
                DrawLine(cylinder, segment, bottom, top, x);
            }
        }

        private static void DrawArc(Cylinder cylinder, CylinderSegment segment, float y)
        {
            Vector3 center = cylinder.transform.TransformPoint(new Vector3(0, y, 0));
            Vector3 forward = cylinder.transform.TransformDirection(
                Quaternion.Euler(0, segment.Rotation - segment.ArcDegrees / 2, 0) *
                Vector3.forward);

            Handles.DrawWireArc(center,
                     cylinder.transform.up,
                     forward,
                     segment.ArcDegrees,
                     cylinder.Radius * cylinder.transform.lossyScale.z
                     , EditorConstants.LINE_THICKNESS
                     );
        }

        private static void DrawLine(Cylinder cylinder, CylinderSegment segment,
            float bottom, float top, float deg)
        {
            Vector3 forward = Quaternion.Euler(0, segment.Rotation + deg, 0) *
                Vector3.forward * cylinder.Radius;

            Vector3 p1 = cylinder.transform.TransformPoint((Vector3.up * bottom) + forward);
            Vector3 p2 = cylinder.transform.TransformPoint((Vector3.up * top) + forward);

            Handles.DrawLine(p1, p2, EditorConstants.LINE_THICKNESS);
        }
    }
}
