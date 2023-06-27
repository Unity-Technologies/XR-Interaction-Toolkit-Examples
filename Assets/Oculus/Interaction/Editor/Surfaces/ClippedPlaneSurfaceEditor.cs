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
using Oculus.Interaction.Surfaces;
using UnityEngine;
using System.Linq;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(ClippedPlaneSurface))]
    public class ClippedPlaneSurfaceEditor : UnityEditor.Editor, IRemoteDrawable
    {
        private ClippedPlaneSurface ClippedPlane =>
            target as ClippedPlaneSurface;

        private void OnSceneGUI()
        {
            if (ClippedPlane.BackingSurface == null ||
                ClippedPlane.Transform == null)
            {
                return;
            }

            if (ClippedPlane.GetClippers()
                .Where(c => c != null)
                .Count() > 0)
            {
                DrawClippingVolumes();
                DrawClippedArea();
            }
        }

        public void DrawRemote()
        {
            DrawClippedArea();
        }

        private void DrawClippingVolumes()
        {
            var prevMatrix = Handles.matrix;
            Handles.matrix = ClippedPlane.Transform.localToWorldMatrix;

            // Draw each clipping area
            foreach (var clipVolume in ClippedPlane.GetClippers()
                .Where((c) => c != null))
            {
                Handles.color = Color.gray;
                if ((clipVolume as IBoundsClipper).
                    GetLocalBounds(ClippedPlane.Transform, out Bounds bounds))
                {
                    Handles.DrawWireCube(bounds.center, bounds.size);
                }
            }

            Handles.matrix = prevMatrix;
        }

        private void DrawClippedArea()
        {
            Bounds maxSize = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
            if (!ClippedPlane.ClipBounds(maxSize, out Bounds bounds))
            {
                return;
            }

            var prevMatrix = Handles.matrix;
            Handles.matrix = ClippedPlane.Transform.localToWorldMatrix;

            // Draw final clipping area
            Handles.color = Color.white;
            Handles.DrawWireCube(bounds.center, bounds.size);

            Handles.color = EditorConstants.PRIMARY_COLOR;
            Vector3 planeMin = new Vector3(bounds.min.x, bounds.min.y, 0);
            Vector3 planeMax = new Vector3(bounds.max.x, bounds.max.y, 0);

            // Draw clipped plane quad
            Handles.DrawLine(planeMin, planeMax);
            Handles.DrawLine(planeMin, new Vector3(planeMin.x, planeMax.y, 0));
            Handles.DrawLine(new Vector3(planeMin.x, planeMax.y, 0), planeMax);
            Handles.DrawLine(planeMax, new Vector3(planeMax.x, planeMin.y, 0));
            Handles.DrawLine(new Vector3(planeMax.x, planeMin.y, 0), planeMin);

            Handles.matrix = prevMatrix;
        }
    }
}
