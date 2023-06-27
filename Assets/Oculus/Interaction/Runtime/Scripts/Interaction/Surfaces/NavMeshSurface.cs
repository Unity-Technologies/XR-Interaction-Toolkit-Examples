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

using UnityEngine;
using UnityEngine.AI;

namespace Oculus.Interaction.Surfaces
{
    public class NavMeshSurface : MonoBehaviour
        , ISurface
    {
        [SerializeField, Optional]
        private string _areaName = string.Empty;
        public string AreaName
        {
            get
            {
                return _areaName;
            }
            set
            {
                _areaName = value;
            }
        }

        [SerializeField, Min(0)]
        private float _snapDistance = 0f;
        public float SnapDistance
        {
            get
            {
                return _snapDistance;
            }
            set
            {
                _snapDistance = value;
            }
        }

        [SerializeField, Min(0)]
        private float _voxelSize = 0.01f;
        public float VoxelSize
        {
            get
            {
                return _voxelSize;
            }
            set
            {
                _voxelSize = Mathf.Max(0, value);
            }
        }

        [SerializeField]
        private bool _calculateNormals = false;
        public bool CalculateHitNormals
        {
            get
            {
                return _calculateNormals;
            }
            set
            {
                _calculateNormals = value;
            }
        }

        public Transform Transform => null;
        private int _areaMask;

        protected virtual void Start()
        {
            if (!string.IsNullOrEmpty(AreaName))
            {
                _areaMask = 1 << NavMesh.GetAreaFromName(AreaName);
            }
            else
            {
                _areaMask = NavMesh.AllAreas;
            }

        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit surfaceHit, float maxDistance = 0)
        {
            if (NavMesh.SamplePosition(point, out NavMeshHit navMeshHit, maxDistance + SnapDistance, _areaMask))
            {
                surfaceHit = new SurfaceHit()
                {
                    Point = navMeshHit.position,
                    Normal = Vector3.up,
                    Distance = navMeshHit.distance
                };
                return true;
            }

            surfaceHit = default(SurfaceHit);
            return false;
        }

        public bool Raycast(in Ray ray, out SurfaceHit surfaceHit, float maxDistance = 0)
        {
            Vector3 dir = ray.direction;
            Vector3 startPoint = ray.origin;
            Vector3 endPoint = startPoint + dir * maxDistance;
            Vector3 projectedDir = Vector3.ProjectOnPlane(endPoint - startPoint, Vector3.up);

            int steps = Mathf.Max(1, Mathf.CeilToInt(projectedDir.magnitude / _voxelSize));
            float stepSize = maxDistance / steps;
            bool found = false;
            surfaceHit = new SurfaceHit();
            surfaceHit.Distance = float.PositiveInfinity;
            Vector3 pos = startPoint + dir * stepSize * 0.5f;
            float size = Mathf.Max(stepSize, SnapDistance);
            float radius = stepSize + Mathf.Sqrt(size * size * 2);
            for (int i = 0; i < steps; i++)
            {
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, radius, _areaMask))
                {
                    float distance = Vector3.Distance(hit.position, pos);
                    if (distance < surfaceHit.Distance)
                    {
                        found = true;
                        surfaceHit.Distance = distance;
                        surfaceHit.Point = hit.position;
                        surfaceHit.Normal = Vector3.up;
                    }
                    else
                    {
                        break;
                    }
                }
                pos += dir * stepSize;
            }

            if (found)
            {
                Vector3 navMeshPoint = surfaceHit.Point;
                Vector3 normal = GetNavMeshNormal(navMeshPoint);
                if (!AlignHits(navMeshPoint, normal, ray, ref surfaceHit, maxDistance))
                {
                    return false;
                }
                return SnapSurfaceHit(ref surfaceHit, navMeshPoint);
            }

            return false;
        }


        private bool AlignHits(Vector3 point, Vector3 normal, Ray ray, ref SurfaceHit surfaceHit, float maxDistance)
        {
            Plane p = new Plane(normal, point);
            if (p.Raycast(ray, out float enter) && enter <= maxDistance)
            {
                surfaceHit.Point = ray.GetPoint(enter);
                return true;
            }
            return false;
        }

        private bool SnapSurfaceHit(ref SurfaceHit surfaceHit, Vector3 navMeshPoint)
        {
            if (NavMesh.Raycast(navMeshPoint, surfaceHit.Point, out NavMeshHit hit, _areaMask))
            {
                float distance = Vector3.Distance(hit.position, surfaceHit.Point);
                surfaceHit.Point = hit.position;
                if (distance > SnapDistance)
                {
                    return false;
                }
            }
            return true;
        }

        private Vector3 GetNavMeshNormal(Vector3 navMeshPoint)
        {
            if (!CalculateHitNormals)
            {
                return Vector3.up;
            }
            Vector3 right = CalculateTangent(Vector3.right, navMeshPoint);
            Vector3 forward = CalculateTangent(Vector3.forward, navMeshPoint);

            return Vector3.Cross(forward, right);

            Vector3 CalculateTangent(Vector3 direction, Vector3 centre)
            {
                bool forwardFound = CalculateStep(centre, direction, out Vector3 stepForward);
                bool backwardFound = CalculateStep(centre, -direction, out Vector3 stepBack);

                if (forwardFound && backwardFound)
                {
                    return (stepForward - stepBack).normalized;
                }
                if (forwardFound)
                {
                    return (stepForward - centre).normalized;
                }
                else if (backwardFound)
                {
                    return (centre - stepBack).normalized;
                }
                return direction;
            }

            bool CalculateStep(Vector3 centre, Vector3 stepDir, out Vector3 value)
            {
                if (NavMesh.SamplePosition(centre + stepDir * VoxelSize, out NavMeshHit hit,
                    VoxelSize * 2, _areaMask))
                {
                    value = hit.position;
                    return true;
                }
                else
                {
                    value = Vector3.zero;
                    return false;
                }
            }
        }
    }
}
