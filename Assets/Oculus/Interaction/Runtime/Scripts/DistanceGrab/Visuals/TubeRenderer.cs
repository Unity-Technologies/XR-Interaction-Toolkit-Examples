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

using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Oculus.Interaction
{
    public struct TubePoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float relativeLength;
    }

    /// <summary>
    /// Creates and renders a tube mesh from sequence of points.
    /// </summary>
    public class TubeRenderer : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct VertexLayout
        {
            public Vector3 pos;
            public Color32 color;
            public Vector2 uv;
        }

        [SerializeField]
        private MeshFilter _filter;
        [SerializeField]
        private MeshRenderer _renderer;
        [SerializeField]
        private int _divisions = 6;
        [SerializeField]
        private int _bevel = 4;
        [SerializeField]
        private int _renderQueue = -1;
        public int RenderQueue
        {
            get
            {
                return _renderQueue;
            }
            set
            {
                _renderQueue = value;
            }
        }
        [SerializeField]
        private Vector2 _renderOffset = Vector2.zero;
        public Vector2 RenderOffset
        {
            get
            {
                return _renderOffset;
            }
            set
            {
                _renderOffset = value;
            }
        }

        [SerializeField]
        private float _radius = 0.005f;
        public float Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        [SerializeField]
        private Gradient _gradient;
        public Gradient Gradient
        {
            get
            {
                return _gradient;
            }
            set
            {
                _gradient = value;
            }
        }

        [SerializeField]
        private Color _tint = Color.white;
        public Color Tint
        {
            get
            {
                return _tint;
            }
            set
            {
                _tint = value;
            }
        }
        [SerializeField, Range(0f, 1f)]
        private float _progressFade = 0.2f;
        public float ProgressFade
        {
            get
            {
                return _progressFade;
            }
            set
            {
                _progressFade = value;
            }
        }
        [SerializeField]
        private float _startFadeThresold = 0.2f;
        public float StartFadeThresold
        {
            get
            {
                return _startFadeThresold;
            }
            set
            {
                _startFadeThresold = value;
            }
        }
        [SerializeField]
        private float _endFadeThresold = 0.2f;
        public float EndFadeThresold
        {
            get
            {
                return _endFadeThresold;
            }
            set
            {
                _endFadeThresold = value;
            }
        }
        [SerializeField]
        private bool _invertThreshold = false;
        public bool InvertThreshold
        {
            get
            {
                return _invertThreshold;
            }
            set
            {
                _invertThreshold = value;
            }
        }
        [SerializeField]
        private float _feather = 0.2f;
        public float Feather
        {
            get
            {
                return _feather;
            }
            set
            {
                _feather = value;
            }
        }

        [SerializeField]
        private bool _mirrorTexture;
        public bool MirrorTexture
        {
            get
            {
                return _mirrorTexture;
            }
            set
            {
                _mirrorTexture = value;
            }
        }

        public float Progress { get; set; } = 0f;
        public float TotalLength => _totalLength;

        private VertexAttributeDescriptor[] _dataLayout;
        private NativeArray<VertexLayout> _vertsData;
        private VertexLayout _layout = new VertexLayout();
        private Mesh _mesh;
        private int[] _tris;
        private int _initializedSteps = -1;

        private float _totalLength = 0f;

        private static readonly int _fadeLimitsShaderID = Shader.PropertyToID("_FadeLimit");
        private static readonly int _fadeSignShaderID = Shader.PropertyToID("_FadeSign");
        private static readonly int _offsetFactorShaderPropertyID = Shader.PropertyToID("_OffsetFactor");
        private static readonly int _offsetUnitsShaderPropertyID = Shader.PropertyToID("_OffsetUnits");

        #region Editor events

        protected virtual void Reset()
        {
            _filter = this.GetComponent<MeshFilter>();
            _renderer = this.GetComponent<MeshRenderer>();
        }

        #endregion

        protected virtual void OnDestroy()
        {
            if (_initializedSteps != -1)
            {
                _vertsData.Dispose();
            }
        }

        protected virtual void OnEnable()
        {
            _renderer.enabled = true;
        }

        protected virtual void OnDisable()
        {
            _renderer.enabled = false;
        }

        /// <summary>
        /// Updates the mesh data for the tube with  the specified points
        /// </summary>
        /// <param name="points">The points that the tube must follow</param>
        /// <param name="space">Indicates if the points are specified in local space or world space</param>
        public void RenderTube(TubePoint[] points, Space space = Space.Self)
        {
            int steps = points.Length;
            if (steps != _initializedSteps)
            {
                InitializeMeshData(steps);
                _initializedSteps = steps;
            }
            UpdateMeshData(points, space);
            _renderer.enabled = enabled;
        }

        /// <summary>
        /// Hides the renderer of the tube
        /// </summary>
        public void Hide()
        {
            _renderer.enabled = false;
        }

        private void InitializeMeshData(int steps)
        {
            if (_vertsData.IsCreated)
            {
                _vertsData.Dispose();
            }
            _dataLayout = new VertexAttributeDescriptor[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };

            int vertsCount = SetVertexCount(steps, _divisions, _bevel);
            SubMeshDescriptor submeshDesc = new SubMeshDescriptor(0, _tris.Length, MeshTopology.Triangles);

            _vertsData = new NativeArray<VertexLayout>(vertsCount, Allocator.Persistent);
            _mesh = new Mesh();
            _mesh.SetVertexBufferParams(vertsCount, _dataLayout);

            _mesh.SetIndexBufferParams(_tris.Length, IndexFormat.UInt32);
            _mesh.SetIndexBufferData(_tris, 0, 0, _tris.Length);
            _mesh.subMeshCount = 1;
            _mesh.SetSubMesh(0, submeshDesc);

            _filter.mesh = _mesh;
        }

        private void UpdateMeshData(TubePoint[] points, Space space)
        {
            int steps = points.Length;
            float totalLength = 0f;
            Vector3 prevPoint = Vector3.zero;
            Pose pose = Pose.identity;
            Pose start = Pose.identity;
            Pose end = Pose.identity;

            Pose rootPose = this.transform.GetPose(Space.World);
            Quaternion inverseRootRotation = Quaternion.Inverse(rootPose.rotation);
            Vector3 rootPositionScaled = new Vector3(
                rootPose.position.x / this.transform.lossyScale.x,
                rootPose.position.y / this.transform.lossyScale.y,
                rootPose.position.z / this.transform.lossyScale.z);
            float uniformScale = space == Space.World ? this.transform.lossyScale.x : 1f;

            TransformPose(points[0], ref start);
            TransformPose(points[points.Length - 1], ref end);

            BevelCap(start, false, 0);

            for (int i = 0; i < steps; i++)
            {
                TransformPose(points[i], ref pose);
                Vector3 point = pose.position;
                Quaternion rotation = pose.rotation;

                float progress = points[i].relativeLength;
                Color color = Gradient.Evaluate(progress) * _tint;

                if (i > 0)
                {
                    totalLength += Vector3.Distance(point, prevPoint);
                }
                prevPoint = point;

                if (i / (steps - 1f) < Progress)
                {
                    color.a *= ProgressFade;
                }

                _layout.color = color;

                WriteCircle(point, rotation, _radius, i + _bevel, progress);
            }

            BevelCap(end, true, _bevel + steps);

            _mesh.bounds = new Bounds(
                (start.position + end.position) * 0.5f,
                end.position - start.position);
            _mesh.SetVertexBufferData(_vertsData, 0, 0, _vertsData.Length, 0, MeshUpdateFlags.DontRecalculateBounds);

            _totalLength = totalLength * uniformScale;

            RedrawFadeThresholds();

            void TransformPose(in TubePoint tubePoint, ref Pose pose)
            {
                if (space == Space.Self)
                {
                    pose.position = tubePoint.position;
                    pose.rotation = tubePoint.rotation;
                    return;
                }

                pose.position = inverseRootRotation * (tubePoint.position - rootPositionScaled);
                pose.rotation = inverseRootRotation * tubePoint.rotation;
            }
        }

        /// <summary>
        /// Resubmits the fading thresholds data to the material without re-generating the mesh
        /// </summary>
        public void RedrawFadeThresholds()
        {
            float originFadeIn = StartFadeThresold / _totalLength;
            float originFadeOut = (StartFadeThresold + Feather) / _totalLength;
            float endFadeIn = (_totalLength - EndFadeThresold) / _totalLength;
            float endFadeOut = (_totalLength - EndFadeThresold - Feather) / _totalLength;

            _renderer.material.SetVector(_fadeLimitsShaderID, new Vector4(
                _invertThreshold ? originFadeOut : originFadeIn,
                _invertThreshold ? originFadeIn : originFadeOut,
                endFadeOut,
                endFadeIn));
            _renderer.material.SetFloat(_fadeSignShaderID, _invertThreshold ? -1 : 1);
            _renderer.material.renderQueue = _renderQueue;

            _renderer.material.SetFloat(_offsetFactorShaderPropertyID, _renderOffset.x);
            _renderer.material.SetFloat(_offsetUnitsShaderPropertyID, _renderOffset.y);
        }

        private void BevelCap(in Pose pose, bool end, int indexOffset)
        {
            Vector3 origin = pose.position;
            Quaternion rotation = pose.rotation;
            for (int i = 0; i < _bevel; i++)
            {
                float radiusFactor = Mathf.InverseLerp(-1, _bevel + 1, i);
                if (end)
                {
                    radiusFactor = 1 - radiusFactor;
                }
                float positionFactor = Mathf.Sqrt(1 - radiusFactor * radiusFactor);
                Vector3 point = origin + (end ? 1 : -1) * (rotation * Vector3.forward) * _radius * positionFactor;
                WriteCircle(point, rotation, _radius * radiusFactor, i + indexOffset, end ? 1 : 0);
            }
        }

        private void WriteCircle(Vector3 point, Quaternion rotation, float width, int index, float progress)
        {
            Color color = Gradient.Evaluate(progress) * _tint;
            if (progress < Progress)
            {
                color.a *= ProgressFade;
            }
            _layout.color = color;

            for (int j = 0; j <= _divisions; j++)
            {
                float radius = 2 * Mathf.PI * j / _divisions;
                Vector3 circle = new Vector3(Mathf.Sin(radius), Mathf.Cos(radius), 0);
                Vector3 normal = rotation * circle;

                _layout.pos = point + normal * width;
                if (_mirrorTexture)
                {
                    float x = (j / (float)_divisions) * 2f;
                    if (j >= _divisions * 0.5f)
                    {
                        x = 2 - x;
                    }
                    _layout.uv = new Vector2(x, progress);
                }
                else
                {
                    _layout.uv = new Vector2(j / (float)_divisions, progress);
                }
                int vertIndex = index * (_divisions + 1) + j;
                _vertsData[vertIndex] = _layout;
            }
        }

        private int SetVertexCount(int positionCount, int divisions, int bevelCap)
        {
            bevelCap = bevelCap * 2;
            int vertsPerPosition = divisions + 1;
            int vertCount = (positionCount + bevelCap) * vertsPerPosition;

            int tubeTriangles = (positionCount - 1 + bevelCap) * divisions * 6;
            int capTriangles = (divisions - 2) * 3;
            int triangleCount = tubeTriangles + capTriangles * 2;
            _tris = new int[triangleCount];

            // handle triangulation
            for (int i = 0; i < positionCount - 1 + bevelCap; i++)
            {
                // add faces
                for (int j = 0; j < divisions; j++)
                {
                    int vert0 = i * vertsPerPosition + j;
                    int vert1 = (i + 1) * vertsPerPosition + j;
                    int t = (i * divisions + j) * 6;
                    _tris[t] = vert0;
                    _tris[t + 1] = _tris[t + 4] = vert1;
                    _tris[t + 2] = _tris[t + 3] = vert0 + 1;
                    _tris[t + 5] = vert1 + 1;
                }
            }

            // triangulate the ends
            Cap(tubeTriangles, 0, divisions - 1, true);
            Cap(tubeTriangles + capTriangles, vertCount - divisions, vertCount - 1);

            void Cap(int t, int firstVert, int lastVert, bool clockwise = false)
            {
                for (int i = firstVert + 1; i < lastVert; i++)
                {
                    _tris[t++] = firstVert;
                    _tris[t++] = clockwise ? i : i + 1;
                    _tris[t++] = clockwise ? i + 1 : i;
                }
            }

            return vertCount;
        }

        #region Inject
        public void InjectAllTubeRenderer(MeshFilter filter,
            MeshRenderer renderer, int divisions, int bevel)
        {
            InjectFilter(filter);
            InjectRenderer(renderer);
            InjectDivisions(divisions);
            InjectBevel(bevel);
        }
        public void InjectFilter(MeshFilter filter)
        {
            _filter = filter;
        }
        public void InjectRenderer(MeshRenderer renderer)
        {
            _renderer = renderer;
        }
        public void InjectDivisions(int divisions)
        {
            _divisions = divisions;
        }
        public void InjectBevel(int bevel)
        {
            _bevel = bevel;
        }

        #endregion
    }
}
