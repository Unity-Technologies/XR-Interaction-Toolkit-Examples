using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRBuilder.Core.Utils
{
    /// <summary>
    /// Bezier curve formulas.
    /// </summary>
    public static class Bezier
    {
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p2;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return
                2f * (1f - t) * (p1 - p0) +
                2f * t * (p2 - p1);
        }

        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float OneMinusT = 1f - t;
            return
                OneMinusT * OneMinusT * OneMinusT * p0 +
                3f * OneMinusT * OneMinusT * t * p1 +
                3f * OneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        public static IEnumerable<float> GetArcLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float length = 0f;
            List<float> lengths = new List<float>();
            lengths.Add(0f);

            for (int i = 0; i <= resolution; ++i)
            {
                points.Add(GetPoint(p0, p1, p2, p3, i / (float)resolution));
            }

            for (int i = 0; i < points.Count - 1; ++i)
            {
                length += Vector3.Distance(points[i], points[i + 1]);
                lengths.Add(length);   
            }

            return lengths;
        }
    }
}
