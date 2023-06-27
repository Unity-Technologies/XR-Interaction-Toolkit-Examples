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
using System.Collections.Generic;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(ClippedCylinderSurface))]
    public class ClippedCylinderSurfaceEditor : UnityEditor.Editor, IRemoteDrawable
    {
        private ClippedCylinderSurface ClippedCylinder =>
            target as ClippedCylinderSurface;

        public void DrawRemote()
        {
            DrawClippers();
            DrawClippedArea();
        }

        private void OnSceneGUI()
        {
            if (ClippedCylinder.BackingSurface == null ||
                ClippedCylinder.Transform == null)
            {
                return;
            }

            DrawClippers();
            DrawClippedArea();
        }

        private void DrawClippers()
        {
            IEnumerable<ICylinderClipper> activeClippers =
                ClippedCylinder.GetClippers()
                .Where(c => c != null);

            if (activeClippers.Count() > 0)
            {
                foreach (var clipper in activeClippers)
                {
                    if (clipper.GetCylinderSegment(out CylinderSegment segment))
                    {
                        Handles.color = Color.gray;
                        SurfaceDrawing.DrawCylinderSegment(
                            ClippedCylinder.Cylinder, segment);
                    }
                }
            }
        }

        private void DrawClippedArea()
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            if (ClippedCylinder.GetClipped(
                out CylinderSegment clipped))
            {
                SurfaceDrawing.DrawCylinderSegment(
                    ClippedCylinder.Cylinder, clipped);
            }
        }
    }
}
