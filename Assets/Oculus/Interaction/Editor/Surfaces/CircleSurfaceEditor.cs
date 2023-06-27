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
    [CustomEditor(typeof(CircleSurface), true)]
    public class CircleSurfaceEditor : UnityEditor.Editor
    {
        private SerializedProperty _planeSurfaceProperty;
        private SerializedProperty _radiusProperty;

        private void Awake()
        {
            _planeSurfaceProperty = serializedObject.FindProperty("_planeSurface");
            _radiusProperty = serializedObject.FindProperty("_radius");
        }

        public void OnSceneGUI()
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            ISurface planeSurface = _planeSurfaceProperty.objectReferenceValue as PlaneSurface;

            if (planeSurface == null)
            {
                return;
            }

            Transform transform = planeSurface.Transform;
            float radius = _radiusProperty.floatValue * transform.lossyScale.x;
            Handles.DrawWireDisc(transform.position, -transform.forward, radius, EditorConstants.LINE_THICKNESS);
        }
    }
}
