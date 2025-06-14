// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace NorthStar
{
    public class HarpoonTrail : MonoBehaviour
    {
        public List<Node> Points = new();
        private static List<Matrix4x4> s_batches = new(1023);
        [SerializeField] private AnimationCurve m_sizeCurve;
        [SerializeField] private Material m_material;
        [SerializeField] private Mesh m_mesh;
        public bool RecordPositions = true;
        private bool m_boatExists = false;
        private void Start()
        {
            m_boatExists = false;
        }

        private void Update()
        {
            if (RecordPositions)
                Points.Add(new Node() { Point = transform.position, Time = Time.time });
            while (Points.Count > 0)
            {
                var node = Points[0];
                var dt = Time.time - node.Time;
                var scale = m_sizeCurve.Evaluate(dt);
                if (scale <= 0)
                    Points.RemoveAt(0);
                else
                    break;
            }

            s_batches.Clear();
            foreach (var node in Points)
            {
                var dt = Time.time - node.Time;
                var scale = m_sizeCurve.Evaluate(dt);
                var matrix = Matrix4x4.TRS(GetPoint(node.Point), Quaternion.identity, Vector3.one * scale);
                s_batches.Add(matrix);
                if (s_batches.Count == 1023)
                    Draw();
            }
            Draw();
        }

        private Vector3 GetPoint(Vector3 point)
        {
            return point;
        }

        private void Draw()
        {
            Graphics.DrawMeshInstanced(m_mesh, 0, m_material, s_batches);
            s_batches.Clear();
        }

        public struct Node
        {
            public Vector3 Point;
            public float Time;
        }
    }
}
