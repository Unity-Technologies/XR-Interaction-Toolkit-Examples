// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Meta.Utilities.Ropes
{
    /// <summary>
    /// Rendering component for 3D tubes along a path, with spline-based subdivision support
    /// Additional settings can be used to create creases and twists, which result in more convincing rope-like geometry, especially in VR
    /// Burst jobs are used for speed up the mesh creation process
    /// </summary>
    [BurstCompile]
    public class TubeRenderer : MonoBehaviour
    {
        [SerializeField, Tooltip("The maximum point capacity of the tube renderer")] private int m_capacity = 200;
        [SerializeField, Tooltip("The radius of the tube renderer at each cross section")] private float m_radius;
        [SerializeField, Tooltip("The points making up the tube")] private List<Vector3> m_points;
        [SerializeField, Min(3), Tooltip("The number of points making up each cross section, with 3 being the minumum (a triangle)")] private int m_resolution;
        [SerializeField, Min(1), Tooltip("The subdivision level used for spline-based smoothing (1 being no subdivisions)")] private int m_subDivisions;
        [SerializeField, Tooltip("The top cap of the tube")] private Transform m_topMesh;
        [SerializeField, Tooltip("The bottom cap of the tube")] private Transform m_bottomMesh;
        [SerializeField, Range(0, 1), Tooltip("Controls the parameterization of the Catmull-Rom curve")] private float m_splineAlpha;
        [SerializeField] private bool m_flipUvs;
        [SerializeField] private UvMode m_uvMode;

        [SerializeField, Tooltip("Additional control that adds bumps/creases around each cross section")] private float m_bumpScale;
        [SerializeField, Tooltip("Additional control that adds bumps/creases around each cross section")] private float m_bumpRadius;
        [SerializeField, Tooltip("Additional control twisting along the length of the tube, use with bumpScale and bumpRadius to make rope-like geometry")] private float m_twist;

        public float Twist
        {
            get => m_twist; set => m_twist = value;
        }

        public int UvIndexOffset { get; set; } = 0;

        private Mesh m_mesh;
        private MeshFilter m_meshFilter;
        private bool m_shouldRecalculate = true, m_shouldReallocate = true, m_running;

        private NativeArray<Vector3> m_pointsForJob;
        private NativeArray<Vector3> m_vertices;
        private NativeArray<Vector3> m_normals;
        private NativeArray<Vector4> m_tangents;
        private NativeArray<Vector2> m_uvs;
        private NativeArray<float> m_preCalceduvs;
        private NativeArray<Quaternion> m_rotations;
        private int[] m_indices;
        private int m_currentJobPointCount;
        private JobHandle m_job;

        private enum UvMode
        {
            Stretch,
            Distribute
        }

        [ContextMenu("Rebuild")]
        public void RunOnce()
        {
            m_mesh = new Mesh();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshFilter.sharedMesh = m_mesh;
            AllocateArrays();
            for (var i = 0; i < m_points.Count; i++)
            {
                m_pointsForJob[i] = m_points[i];
            }
            RecalculateMesh();
            FinishMesh();
            DisposeArrays();
        }

        private void Awake()
        {
            m_mesh = new Mesh();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshFilter.sharedMesh = m_mesh;
            ResizeTube(m_capacity);
        }

        private void DisposeArrays()
        {
            if (m_vertices == null || !m_vertices.IsCreated)
                return;

            m_job.Complete();
            m_vertices.Dispose();
            m_uvs.Dispose();
            m_preCalceduvs.Dispose();
            m_rotations.Dispose();
            m_normals.Dispose();
            m_tangents.Dispose();
            m_pointsForJob.Dispose();
        }
        private void OnDestroy()
        {
            DisposeArrays();
        }

        private void AllocateArrays()
        {
            var size = (m_resolution + 1) * ((m_points.Capacity - 1) * (m_subDivisions - 1) + 1);
            DisposeArrays();
            m_vertices = new NativeArray<Vector3>(size, Allocator.Persistent);
            m_uvs = new NativeArray<Vector2>(size, Allocator.Persistent);
            m_normals = new NativeArray<Vector3>(size, Allocator.Persistent);
            m_tangents = new NativeArray<Vector4>(size, Allocator.Persistent);
            m_indices = new int[6 * m_resolution * (m_points.Capacity - 1) * (m_subDivisions - 1)];
            m_pointsForJob = new NativeArray<Vector3>(m_points.Capacity, Allocator.Persistent);
            m_preCalceduvs = new NativeArray<float>(m_points.Capacity, Allocator.Persistent);
            m_rotations = new NativeArray<Quaternion>(m_points.Capacity, Allocator.Persistent);

            for (var i = 0; i < (m_points.Capacity - 1) * (m_subDivisions - 1); i++)
            {
                for (var j = 0; j < m_resolution; j++)
                {
                    var bl = i * (m_resolution + 1) + j;
                    var tl = (i + 1) * (m_resolution + 1) + j;
                    var br = bl + 1;
                    var tr = tl + 1;

                    var index = i * m_resolution * 6 + j * 6;

                    Debug.Assert(tr < m_vertices.Length, $"Triangle index out of range");

                    m_indices[index + 0] = br;
                    m_indices[index + 1] = tl;
                    m_indices[index + 2] = bl;
                    m_indices[index + 3] = tr;
                    m_indices[index + 4] = tl;
                    m_indices[index + 5] = br;
                }
            }
        }

        /// <summary>
        /// Recalculate all required parameters and kick-off burst job to be completed asyncronously during the frame
        /// </summary>
        private void RecalculateMesh()
        {
            if (m_points.Count == 0)
            {
                m_topMesh.gameObject.SetActive(false);
                m_bottomMesh.gameObject.SetActive(false);
                return;
            }

            if (m_uvMode == UvMode.Stretch)
            {
                for (var i = 0; i < m_points.Count; i++)
                {
                    m_preCalceduvs[i] = (float)(UvIndexOffset + i) / (UvIndexOffset + m_points.Count);
                }
            }
            else
            {
                var accumulatedVal = 0f;
                for (var i = 0; i < m_points.Count - 1; i++)
                {
                    var distance = Vector3.Distance(m_points[i], m_points[i + 1]);
                    accumulatedVal += distance;

                    m_preCalceduvs[i] = accumulatedVal;
                }
                for (var i = 0; i < m_points.Count - 1; i++)
                {
                    m_preCalceduvs[i] /= accumulatedVal;
                }
                m_preCalceduvs[^1] = 1f;
            }

            m_rotations[0] = Quaternion.identity;
            var prevNormal = m_rotations[0] * Vector3.forward;
            for (var i = 1; i < m_points.Count; i++)
            {
                var dir = (m_points[i] - m_points[i - 1]).normalized;
                var dir2 = Vector3.Cross(dir, prevNormal).normalized;
                prevNormal = Vector3.Cross(dir2, dir).normalized;
                m_rotations[i] = prevNormal.sqrMagnitude > Mathf.Epsilon && dir.sqrMagnitude > Mathf.Epsilon ? Quaternion.LookRotation(prevNormal, dir) : Quaternion.identity;
            }

            m_topMesh.gameObject.SetActive(true);
            m_bottomMesh.gameObject.SetActive(true);
            m_topMesh.localScale = Vector3.one * m_radius;
            m_bottomMesh.localScale = Vector3.one * m_radius;
            m_topMesh.localPosition = m_points[^1];
            m_topMesh.localRotation = m_rotations[m_points.Count - 1];
            m_bottomMesh.localPosition = m_points[0];
            m_bottomMesh.localRotation = m_rotations[0];

            m_currentJobPointCount = m_points.Count;

            var job = new UpdateMeshJob()
            {
                Points = m_pointsForJob,
                Vertices = m_vertices,
                Normals = m_normals,
                Tangents = m_tangents,
                Uvs = m_uvs,
                PrecalcedUv = m_preCalceduvs,
                Rotations = m_rotations,
                Radius = m_radius,
                BumpScale = m_bumpScale,
                BumpRadius = m_bumpRadius,
                Twist = m_twist,
                Resolution = m_resolution,
                SubDivisions = m_subDivisions - 1,
                SplineAlpha = m_splineAlpha,
                Count = m_currentJobPointCount
            };

            m_job = job.Schedule((m_currentJobPointCount - 1) * (m_subDivisions - 1) + 1, 32);
            m_running = true;
        }

        /// <summary>
        /// Finalise the burst job and transfer mesh data to the mesh itself
        /// This could be improved by using MeshData // NativeArrays
        /// </summary>
        private void FinishMesh()
        {
            m_job.Complete();
            m_mesh.Clear();
            var size = (m_resolution + 1) * ((m_currentJobPointCount - 1) * (m_subDivisions - 1) + 1);
            m_mesh.SetVertices(m_vertices, 0, size);
            m_mesh.SetUVs(0, m_uvs, 0, size);
            m_mesh.SetNormals(m_normals, 0, size);
            m_mesh.SetTangents(m_tangents, 0, size);
            m_mesh.SetTriangles(m_indices, trianglesStart: 0, 6 * m_resolution * (m_currentJobPointCount - 1) * (m_subDivisions - 1), 0);

            m_mesh.RecalculateBounds();
        }

        private void LateUpdate()
        {
            if (m_running)
            {
                FinishMesh();
                m_running = false;
            }
            if (m_shouldReallocate)
            {
                m_shouldReallocate = false;
                AllocateArrays();
            }
            if (m_shouldRecalculate)
            {
                for (var i = 0; i < m_points.Count; i++)
                {
                    m_pointsForJob[i] = m_points[i];
                }
                RecalculateMesh();
                m_shouldRecalculate = false;
            }
        }

        #region Jobs
        [BurstCompile(CompileSynchronously = true)]
        private struct UpdateMeshJob : IJobParallelFor
        {
            [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<Vector3> Points;
            [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<float> PrecalcedUv;
            [ReadOnly, NativeDisableParallelForRestriction] public NativeArray<Quaternion> Rotations;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vector3> Vertices;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vector3> Normals;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vector4> Tangents;
            [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<Vector2> Uvs;

            public int Resolution;
            public int SubDivisions;
            public int Count;
            public float Radius;
            public float SplineAlpha;
            public float BumpScale;
            public float BumpRadius;
            public float Twist;

            public void Execute(int i)
            {
                var pointIndex = i / SubDivisions;
                var nextPointIndex = Mathf.Min(pointIndex + 1, Count - 1);
                var lastPoint = Points[pointIndex];
                var center = lastPoint;
                var subDivProg = i % SubDivisions / (float)SubDivisions;

                if (pointIndex + 1 < Count)
                {
                    var p1 = Points[pointIndex];
                    var p2 = Points[pointIndex + 1];

                    var p0 = pointIndex > 0 ? Points[pointIndex - 1] : p1 + (p2 - p1);
                    var p3 = pointIndex + 2 < Count ? Points[pointIndex + 2] : p2 + (p2 - p1);

                    var spline = new CatmullRomCurve(p0, p1, p2, p3, SplineAlpha);

                    var splinePoint = spline.GetPoint(subDivProg);
                    center = splinePoint;
                }

                var rotation = Quaternion.Slerp(Rotations[pointIndex], Rotations[nextPointIndex], subDivProg);
                var up = rotation * Vector3.up;
                var height = Mathf.Lerp(PrecalcedUv[pointIndex], PrecalcedUv[nextPointIndex], subDivProg);

                for (var j = 0; j < Resolution + 1; j++)
                {
                    var progress = j / (float)Resolution;
                    var dir = rotation * Quaternion.Euler(0, progress * 360 + height * Twist, 0) * Vector3.forward;
                    var point = center + dir * Radius * (1 - BumpRadius + Mathf.Abs(Mathf.Cos(progress * Mathf.PI * BumpScale)) * BumpRadius);
                    var index = i * (Resolution + 1) + j;
                    Vertices[index] = point;
                    Uvs[index] = new Vector2(progress, height);
                    Normals[index] = dir;

                    var tangent = Vector3.Cross(up, dir);
                    Tangents[index] = new Vector4(tangent.x, tangent.y, tangent.z, 1);
                }
            }
        }
        #endregion

        private Vector3 RecalculateUp(int index)
        {
            var p1 = m_points[index];
            if (index > 0)
                p1 = m_points[index - 1];
            var p2 = m_points[index];
            if (index < m_points.Count - 1)
                p2 = m_points[index + 1];

            var up = (p2 - p1).normalized;
            return up;
        }

        public void SetPoint(Vector3 point, int index)
        {
            if (m_points.Capacity <= index)
                ResizeTube(index + 1);
            if (index > m_points.Count - 1)
                m_points.Add(point);
            else
                m_points[index] = point;
            m_shouldRecalculate = true;
        }

        public void ClearPoints()
        {
            m_points.Clear();
        }

        public void ResizeTube(int newSize)
        {
            var newCapacity = Mathf.Max(1, newSize);
            if (m_points.Capacity < newCapacity)
            {
                m_points.Capacity = newCapacity;
                m_shouldReallocate = true;
            }
        }

        public struct CatmullRomCurve
        {
            public Vector3 P0, P1, P2, P3;
            public float Alpha;

            public CatmullRomCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float alpha)
            {
                (P0, P1, P2, P3) = (p0, p1, p2, p3);
                Alpha = alpha;

            }

            // Evaluates a point at the given t-value from 0 to 1
            public Vector3 GetPoint(float t)
            {
                // calculate knots
                const float K0 = 0;
                var k1 = GetKnotInterval(P0, P1);
                var k2 = GetKnotInterval(P1, P2) + k1;
                var k3 = GetKnotInterval(P2, P3) + k2;

                // evaluate the point
                var u = Mathf.LerpUnclamped(k1, k2, t);
                var a1 = Remap(K0, k1, P0, P1, u);
                var a2 = Remap(k1, k2, P1, P2, u);
                var a3 = Remap(k2, k3, P2, P3, u);
                var b1 = Remap(K0, k2, a1, a2, u);
                var b2 = Remap(k1, k3, a2, a3, u);
                return Remap(k1, k2, b1, b2, u);
            }

            private static Vector3 Remap(float a, float b, Vector3 c, Vector3 d, float u)
            {
                return Vector3.LerpUnclamped(c, d, (u - a) / (b - a));
            }

            private float GetKnotInterval(Vector3 a, Vector3 b)
            {
                return Mathf.Pow(Vector3.SqrMagnitude(a - b), 0.5f * Alpha);
            }
        }

    }
}