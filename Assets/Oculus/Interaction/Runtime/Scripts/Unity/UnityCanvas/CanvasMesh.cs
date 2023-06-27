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
using UnityEngine.Profiling;

namespace Oculus.Interaction.UnityCanvas
{
    [DisallowMultipleComponent]
    public abstract class CanvasMesh : MonoBehaviour
    {
        [Tooltip("Mesh construction will be driven by this texture.")]
        [SerializeField]
        protected CanvasRenderTexture _canvasRenderTexture;

        [Tooltip("The mesh filter that will be driven.")]
        [SerializeField]
        protected MeshFilter _meshFilter;

        [Tooltip("Optional mesh collider that will be driven.")]
        [SerializeField, Optional]
        protected MeshCollider _meshCollider = null;

        protected bool _started = false;

        protected abstract Vector3 MeshInverseTransform(Vector3 localPosition);

        protected abstract void GenerateMesh(out List<Vector3> verts, out List<int> tris, out List<Vector2> uvs);

        /// <summary>
        /// Transform a position in world space relative to the imposter to an associated position relative
        /// to the original canvas in world space.
        /// </summary>
        public Vector3 ImposterToCanvasTransformPoint(Vector3 worldPosition)
        {
            Vector3 localToImposter =
                _meshFilter.transform.InverseTransformPoint(worldPosition);
            Vector3 canvasLocalPosition = MeshInverseTransform(localToImposter) /
                                          _canvasRenderTexture.transform.localScale.x;
            Vector3 transformedWorldPosition = _canvasRenderTexture.transform.TransformPoint(canvasLocalPosition);
            return transformedWorldPosition;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_meshFilter, nameof(_meshFilter));
            this.AssertField(_canvasRenderTexture, nameof(_canvasRenderTexture));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                UpdateImposter();

                _canvasRenderTexture.OnUpdateRenderTexture += HandleUpdateRenderTexture;
                if (_canvasRenderTexture.Texture != null)
                {
                    HandleUpdateRenderTexture(_canvasRenderTexture.Texture);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _canvasRenderTexture.OnUpdateRenderTexture -= HandleUpdateRenderTexture;
            }
        }

        protected virtual void HandleUpdateRenderTexture(Texture texture)
        {
            UpdateImposter();
        }

        protected virtual void UpdateImposter()
        {
            Profiler.BeginSample("InterfaceRenderer.UpdateImposter");
            try
            {
                GenerateMesh(out List<Vector3> verts, out List<int> tris, out List<Vector2> uvs);

                Mesh mesh = new Mesh();
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                _meshFilter.mesh = mesh;
                if (_meshCollider != null)
                {
                    _meshCollider.sharedMesh = _meshFilter.sharedMesh;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        #region Inject

        public void InjectAllCanvasMesh(CanvasRenderTexture canvasRenderTexture, MeshFilter meshFilter)
        {
            InjectCanvasRenderTexture(canvasRenderTexture);
            InjectMeshFilter(meshFilter);
        }

        public void InjectCanvasRenderTexture(CanvasRenderTexture canvasRenderTexture)
        {
            _canvasRenderTexture = canvasRenderTexture;
        }

        public void InjectMeshFilter(MeshFilter meshFilter)
        {
            _meshFilter = meshFilter;
        }

        public void InjectOptionalMeshCollider(MeshCollider meshCollider)
        {
            _meshCollider = meshCollider;
        }

        #endregion
    }
}
