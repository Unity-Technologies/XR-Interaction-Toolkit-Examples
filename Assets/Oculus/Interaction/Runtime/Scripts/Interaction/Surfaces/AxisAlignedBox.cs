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

namespace Oculus.Interaction.Surfaces
{
    public class AxisAlignedBox : MonoBehaviour, ISurface
    {
        [SerializeField, Tooltip("Size of the axis-aligned box, default to mesh size")]
        private Vector3 _size = new Vector3(0.0f, 0.0f, 0.0f);
        private readonly Dictionary<BoxSurface, float> _distances = new Dictionary<BoxSurface, float>()
        {
            { BoxSurface.XMin, 0 },
            { BoxSurface.YMin, 0 },
            { BoxSurface.ZMin, 0 },
            { BoxSurface.XMax, 0 },
            { BoxSurface.YMax, 0 },
            { BoxSurface.ZMax, 0 },
        };

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

        public Transform Transform => transform;

        public Bounds Bounds => new Bounds(transform.position, _size);

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();
            Vector3 boundPoint = Vector3.Min(Vector3.Max(point, Bounds.min), Bounds.max);
            var closestSide = FindClosestBoxSide(point);
            hit.Normal = ClosestSurfaceNormal(point, closestSide);
            if (!IsWithinVolume(point))
            {
                hit.Point = boundPoint;
                hit.Distance = (point - boundPoint).magnitude;
                return maxDistance <= 0 || hit.Distance <= maxDistance;
            }

            // if boundPoint is inside the Axis-Aligned box, push to boundary
            switch (closestSide)
            {
                case BoxSurface.XMin:
                    boundPoint.x = Bounds.min.x;
                    break;
                case BoxSurface.YMin:
                    boundPoint.y = Bounds.min.y;
                    break;
                case BoxSurface.ZMin:
                    boundPoint.z = Bounds.min.z;
                    break;
                case BoxSurface.XMax:
                    boundPoint.x = Bounds.max.x;
                    break;
                case BoxSurface.YMax:
                    boundPoint.y = Bounds.max.y;
                    break;
                case BoxSurface.ZMax:
                    boundPoint.z = Bounds.max.z;
                    break;
            }

            hit.Point = boundPoint;
            hit.Distance = Vector3.Distance(hit.Point, point);
            if (maxDistance > 0 && hit.Distance > maxDistance)
            {
                return false;
            }
            return true;
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();

            Vector3 dirInv = new Vector3(1.0f / ray.direction.x, 1.0f / ray.direction.y, 1.0f / ray.direction.z);

            Vector3 vecMin = Vector3.Scale(Bounds.min - ray.origin, dirInv);
            Vector3 vecMax = Vector3.Scale(Bounds.max - ray.origin, dirInv);

            float tmin = Mathf.Max(Mathf.Max(Mathf.Min(vecMin.x, vecMax.x), Mathf.Min(vecMin.y, vecMax.y)), Mathf.Min(vecMin.z, vecMax.z));
            float tmax = Mathf.Min(Mathf.Min(Mathf.Max(vecMin.x, vecMax.x), Mathf.Max(vecMin.y, vecMax.y)), Mathf.Max(vecMin.z, vecMax.z));

            // box is behind the ray origin
            if (tmax < 0)
            {
                hit.Distance = tmax;
                return false;
            }

            // ray does not intersect the box
            if (tmin > tmax)
            {
                hit.Distance = tmax;
                return false;
            }

            hit.Distance = tmin;

            // too far
            if (maxDistance > 0 && hit.Distance > maxDistance)
            {
                return false;
            }

            // ray origin inside the box
            if (Mathf.Sign(tmin) != Mathf.Sign(tmax))
            {
                hit.Distance = Mathf.Max(tmax, tmin);
            }

            hit.Point = ray.origin + ray.direction * hit.Distance;
            hit.Normal = ClosestSurfaceNormal(hit.Point);
            return true;
        }

        protected void Start()
        {
            if (GetComponent<MeshFilter>())
            {
                // get the local size of mesh as the size of axis-aligned box; does not account for the scale of parent objects
                _size = Vector3.Scale(transform.localScale, GetComponent<MeshFilter>().mesh.bounds.size);
            }
            if (_size.magnitude == 0.0f)
            {
                // if no mesh is found, default to 0.1f
                _size = new Vector3(0.1f, 0.1f, 0.1f);
            }
            Size = _size;
        }

        private bool IsWithinVolume(Vector3 point) => Bounds.Contains(point);

        private BoxSurface FindClosestBoxSide(Vector3 point)
        {
            Vector3 pointRef = transform.position - point;
            Vector3 halfSize = Bounds.extents;

            _distances[BoxSurface.XMin] = halfSize.x - pointRef.x;
            _distances[BoxSurface.YMin] = halfSize.y - pointRef.y;
            _distances[BoxSurface.ZMin] = halfSize.z - pointRef.z;
            _distances[BoxSurface.XMax] = halfSize.x + pointRef.x;
            _distances[BoxSurface.YMax] = halfSize.y + pointRef.y;
            _distances[BoxSurface.ZMax] = halfSize.z + pointRef.z;

            var closest = BoxSurface.XMin;
            foreach (var key in _distances.Keys)
            {
                if (_distances[key] < _distances[closest])
                {
                    closest = key;
                }
            }
            return closest;
        }

        private Vector3 ClosestSurfaceNormal(Vector3 point, BoxSurface? side = null) =>
            (side ?? FindClosestBoxSide(point)) switch
            {
                BoxSurface.XMin => new Vector3(-1.0f, 0.0f, 0.0f),
                BoxSurface.YMin => new Vector3(0.0f, -1.0f, 0.0f),
                BoxSurface.ZMin => new Vector3(0.0f, 0.0f, -1.0f),
                BoxSurface.XMax => new Vector3(1.0f, 0.0f, 0.0f),
                BoxSurface.YMax => new Vector3(0.0f, 1.0f, 0.0f),
                BoxSurface.ZMax => new Vector3(0.0f, 0.0f, 1.0f),
                _ => throw new System.NotImplementedException(),
            };

        private enum BoxSurface
        {
            XMin,
            YMin,
            ZMin,
            XMax,
            YMax,
            ZMax,
        }
    }
}
