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

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Oculus.Interaction.Surfaces;
using UnityEngine.SceneManagement;
using System.Linq;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(BoundsClipper))]
    public class BoundsClipperEditor : UnityEditor.Editor
    {
        private bool _visualize = false;

        private IEnumerable<IRemoteDrawable> _drawables;

        private BoundsClipper Clipper => target as BoundsClipper;

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(_visualize ? "Hide Surface Visuals" : "Show Surface Visuals"))
            {
                _visualize = !_visualize;
                _drawables = null;
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        private void UpdateDrawables()
        {
            _drawables = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Union(new[] { Clipper.transform.root.gameObject })
                .SelectMany(root => root
                .GetComponentsInChildren<IClippedSurface<IBoundsClipper>>(false))
                .Where((s) => s.GetClippers().Contains(Clipper))
                .Select(s => CreateEditor(s as Object) as IRemoteDrawable);
        }

        private void OnSceneGUI()
        {
            if (!Clipper.GetLocalBounds(Clipper.transform, out Bounds localBounds))
            {
                return;
            }

            var prevColor = Handles.color;
            var prevMatrix = Handles.matrix;
            Handles.color = _visualize ? Color.white : Color.white;
            Handles.matrix = Clipper.transform.localToWorldMatrix;
            Handles.DrawWireCube(localBounds.center, localBounds.size);
            Handles.color = prevColor;
            Handles.matrix = prevMatrix;

            if (_visualize)
            {
                if (_drawables == null)
                {
                    UpdateDrawables();
                }
                foreach (var drawer in _drawables)
                {
                    drawer.DrawRemote();
                }
            }
        }
    }
}
