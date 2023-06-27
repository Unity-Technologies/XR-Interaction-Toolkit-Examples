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
    [CustomEditor(typeof(PokeInteractable))]
    public class PokeInteractableEditor : SimplifiedEditor
    {
        private PokeInteractable _interactable;

        private SerializedProperty _surfaceProperty;

        private static readonly float DRAW_RADIUS = 0.02f;

        private void Awake()
        {
            _interactable = target as PokeInteractable;

            _surfaceProperty = serializedObject.FindProperty("_surfacePatch");
        }

        public void OnSceneGUI()
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            ISurfacePatch surfacePatch = _surfaceProperty.objectReferenceValue as ISurfacePatch;

            if (surfacePatch == null)
            {
                // Currently only supports visualizing planar surfaces
                return;
            }

            Transform triggerPlaneTransform = surfacePatch.Transform;

            if (triggerPlaneTransform == null)
            {
                return;
            }

            Vector3 touchPoint = triggerPlaneTransform.position - triggerPlaneTransform.forward * _interactable.EnterHoverNormal;
            surfacePatch.ClosestSurfacePoint(touchPoint, out SurfaceHit hit);
            Vector3 proximalPoint = hit.Point;

            Handles.DrawSolidDisc(touchPoint, triggerPlaneTransform.forward, DRAW_RADIUS);

#if UNITY_2020_2_OR_NEWER
            Handles.DrawLine(touchPoint, proximalPoint, EditorConstants.LINE_THICKNESS);

            Handles.DrawLine(proximalPoint - triggerPlaneTransform.right * DRAW_RADIUS,
                proximalPoint + triggerPlaneTransform.right * DRAW_RADIUS, EditorConstants.LINE_THICKNESS);
            Handles.DrawLine(proximalPoint - triggerPlaneTransform.up * DRAW_RADIUS,
                proximalPoint + triggerPlaneTransform.up * DRAW_RADIUS, EditorConstants.LINE_THICKNESS);
#else
            Handles.DrawLine(touchPoint, proximalPoint);

            Handles.DrawLine(proximalPoint - triggerPlaneTransform.right * DRAW_RADIUS,
                proximalPoint + triggerPlaneTransform.right * DRAW_RADIUS);
            Handles.DrawLine(proximalPoint - triggerPlaneTransform.up * DRAW_RADIUS,
                proximalPoint + triggerPlaneTransform.up * DRAW_RADIUS);
#endif
        }
    }
}
