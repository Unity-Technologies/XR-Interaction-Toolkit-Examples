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
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(PlaneSurface))]
    public class PlaneSurfaceEditor : UnityEditor.Editor
    {
        private const int NUM_SEGMENTS = 40;
        private const float FADE_DISTANCE = 10f;

        private static readonly Color Color = EditorConstants.PRIMARY_COLOR_DISABLED;

        private static float Interval => FADE_DISTANCE / NUM_SEGMENTS;

        public void OnSceneGUI()
        {
            PlaneSurface plane = target as PlaneSurface;
            Draw(plane);
        }

        public static void Draw(PlaneSurface plane)
        {
            Vector3 origin = plane.transform.position;

            if (SceneView.lastActiveSceneView?.camera != null)
            {
                Transform camTransform = SceneView.lastActiveSceneView.camera.transform;
                if (plane.ClosestSurfacePoint(camTransform.position, out SurfaceHit hit, 0))
                {
                    Vector3 hitDelta = PoseUtils.Delta(plane.transform, new Pose(hit.Point, plane.transform.rotation)).position;
                    hitDelta.x = Mathf.RoundToInt(hitDelta.x / Interval) * Interval;
                    hitDelta.y = Mathf.RoundToInt(hitDelta.y / Interval) * Interval;
                    hitDelta.z = 0f;
                    origin = PoseUtils.Multiply(plane.transform.GetPose(), new Pose(hitDelta, Quaternion.identity)).position;
                }
            }

            DrawLines(origin, plane.Normal, plane.transform.up, Color);
            DrawLines(origin, plane.Normal, -plane.transform.up, Color);
            DrawLines(origin, plane.Normal, plane.transform.right, Color);
            DrawLines(origin, plane.Normal, -plane.transform.right, Color);
        }

        private static void DrawLines(in Vector3 origin,
                                      in Vector3 normal,
                                      in Vector3 direction,
                                      in Color baseColor)
        {
            Color prevColor = Handles.color;
            Color color = baseColor;
            Vector3 offsetOrigin = origin;

            for (int i = 0; i < NUM_SEGMENTS; ++i)
            {
                Handles.color = color;

                Vector3 cross = Vector3.Cross(normal, direction).normalized;
                float interval = Interval;

                for (int j = -NUM_SEGMENTS; j < NUM_SEGMENTS; ++j)
                {
                    float horizStart = interval * j;
                    float horizEnd = horizStart + interval;

                    Vector3 start = offsetOrigin + cross * horizStart;
                    Vector3 end = offsetOrigin + cross * horizEnd;

                    color.a = 1f - Mathf.Abs((float)j / NUM_SEGMENTS);
                    color.a *= 1f - ((float)i / NUM_SEGMENTS);
                    color.a *= color.a;

                    Handles.color = color;
                    Handles.DrawLine(start, end);
                }

                offsetOrigin += direction * interval;
                color = baseColor;
            }

            Handles.color = prevColor;
        }
    }
}
