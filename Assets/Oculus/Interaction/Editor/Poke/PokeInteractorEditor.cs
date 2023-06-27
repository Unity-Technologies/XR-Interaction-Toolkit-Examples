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
using UnityEditor.IMGUI.Controls;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(PokeInteractor))]
    public class PokeInteractorEditor : SimplifiedEditor
    {
        private SphereBoundsHandle _sphereHandle = new SphereBoundsHandle();
        private PokeInteractor _interactor;
        private SerializedProperty _pointProperty;

        private void Awake()
        {
            _interactor = target as PokeInteractor;

            _sphereHandle.SetColor(EditorConstants.PRIMARY_COLOR);
            _sphereHandle.midpointHandleDrawFunction = null;

            _pointProperty = serializedObject.FindProperty("_pointTransform");
        }

        private void OnSceneGUI()
        {
            DrawSphereEditor();
        }

        private void DrawSphereEditor()
        {
            Transform transform = _pointProperty.objectReferenceValue as Transform;
            if (transform == null)
            {
                return;
            }
            _sphereHandle.radius = _interactor.Radius;
            _sphereHandle.center = transform.position;
            _sphereHandle.DrawHandle();
        }
    }
}
