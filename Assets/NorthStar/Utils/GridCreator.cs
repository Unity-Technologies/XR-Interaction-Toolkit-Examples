// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Creates a procedural mesh grid
    /// </summary>
    [ExecuteInEditMode]
    public class GridCreator : MonoBehaviour
    {
        [SerializeField] private float m_size = 1.0f;
        [SerializeField] private int m_count = 32;

        private void Awake()
        {
            Generate();
        }

        private void OnValidate()
        {
            Generate();
        }

        private void Generate()
        {
            var vertices = new Vector3[(m_count + 1) * (m_count + 1)];
            var indices = new int[m_count * m_count * 6];

            for (var y = 0; y <= m_count; y++)
            {
                for (var x = 0; x <= m_count; x++)
                {
                    var vertexIndex = y * (m_count + 1) + x;
                    vertices[vertexIndex] = new Vector3(x / (float)m_count * m_size, 0.0f, y / (float)m_count * m_size);

                    // Don't add indices for edges
                    if (x == m_count || y == m_count)
                        continue;
                    _ = y * m_count + x;
                    var isEven = (x & 1) == (y & 1);

                    var triIndex = (y * m_count + x) * 6;

                    // Alternate two index buffer arrangements to reduce diagonal artifacts
                    if (isEven)
                    {
                        indices[triIndex + 0] = (y + 0) * (m_count + 1) + x + 0;
                        indices[triIndex + 1] = (y + 1) * (m_count + 1) + x + 0;
                        indices[triIndex + 2] = (y + 0) * (m_count + 1) + x + 1;

                        indices[triIndex + 3] = (y + 0) * (m_count + 1) + x + 1;
                        indices[triIndex + 4] = (y + 1) * (m_count + 1) + x + 0;
                        indices[triIndex + 5] = (y + 1) * (m_count + 1) + x + 1;
                    }
                    else
                    {
                        indices[triIndex + 0] = (y + 1) * (m_count + 1) + x + 0;
                        indices[triIndex + 1] = (y + 1) * (m_count + 1) + x + 1;
                        indices[triIndex + 2] = (y + 0) * (m_count + 1) + x + 0;

                        indices[triIndex + 3] = (y + 0) * (m_count + 1) + x + 0;
                        indices[triIndex + 4] = (y + 1) * (m_count + 1) + x + 1;
                        indices[triIndex + 5] = (y + 0) * (m_count + 1) + x + 1;
                    }
                }
            }

            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh()
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices,
                triangles = indices
            };
        }
    }
}