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
using System.Linq;

namespace Oculus.Interaction.Surfaces
{
    public class ClippedCylinderSurface : MonoBehaviour, IClippedSurface<ICylinderClipper>
    {
        private const float EPSILON = 0.0001f;

        [Tooltip("The Cylinder Surface to be clipped.")]
        [SerializeField]
        private CylinderSurface _cylinderSurface;

        [Tooltip("The clippers that will be used to clip the Cylinder Surface.")]
        [SerializeField, Interface(typeof(ICylinderClipper))]
        private List<UnityEngine.Object> _clippers;

        public Transform Transform => _cylinderSurface.Transform;

        public ISurface BackingSurface => _cylinderSurface;

        public Cylinder Cylinder => _cylinderSurface.Cylinder;

        public IEnumerable<ICylinderClipper> GetClippers()
        {
            foreach (var clipper in _clippers)
            {
                yield return clipper as ICylinderClipper;
            }
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            return BackingSurface.Raycast(ray, out hit, maxDistance) &&
                   ClosestSurfacePoint(hit.Point, out SurfaceHit clamped, 0) &&
                   hit.Point.Approximately(clamped.Point, EPSILON);
        }

        protected virtual void Start()
        {
            this.AssertField(_cylinderSurface, nameof(_cylinderSurface));
            this.AssertCollectionItems(_clippers, nameof(_clippers));
        }

        public bool GetClipped(out CylinderSegment clipped)
        {
            bool anyClipped = false;
            bool isInfiniteHeight = true;

            float minDeg = float.MinValue;
            float maxDeg = float.MaxValue;
            float bottom = float.MinValue;
            float top = float.MaxValue;

            foreach (var clipperMono in _clippers)
            {
                ICylinderClipper clipper = clipperMono as ICylinderClipper;
                if (clipper == null ||
                    !clipper.GetCylinderSegment(out CylinderSegment segment))
                {
                    continue;
                }

                anyClipped = true;

                float clipMin = segment.Rotation - segment.ArcDegrees / 2f;
                float clipMax = segment.Rotation + segment.ArcDegrees / 2f;

                minDeg = Mathf.Max(minDeg, clipMin);
                maxDeg = Mathf.Min(maxDeg, clipMax);

                if (!segment.IsInfiniteHeight)
                {
                    isInfiniteHeight = false;
                    bottom = Mathf.Max(bottom, segment.Bottom);
                    top = Mathf.Min(top, segment.Top);
                }
            }

            if (!anyClipped)
            {
                clipped = CylinderSegment.Infinite();
                return true;
            }

            if (minDeg > maxDeg || (!isInfiniteHeight && bottom > top))
            {
                clipped = default;
                return false;
            }

            float center = Mathf.Lerp(minDeg, maxDeg, 0.5f) % 360f;
            if (isInfiniteHeight)
            {
                bottom = 1;
                top = -1;
            }
            clipped = new CylinderSegment(
                center, maxDeg - minDeg, bottom, top);

            return true;
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            if (!GetClipped(out CylinderSegment clipped))
            {
                hit = default;
                return false;
            }

            Vector3 localPoint = Cylinder.transform.InverseTransformPoint(point);
            Vector3 localHitPoint = localPoint;
            Vector3 nearestPointOnCenterAxis;

            if (!clipped.IsInfiniteHeight)
            {
                localHitPoint.y = Mathf.Clamp(localHitPoint.y, clipped.Bottom, clipped.Top);
            }

            if (!clipped.IsInfiniteArc)
            {
                float angle = Mathf.Atan2(localHitPoint.x, localHitPoint.z) * Mathf.Rad2Deg % 360;
                float rotation = clipped.Rotation % 360;

                if (angle > rotation + 180)
                {
                    angle -= 360;
                }
                else if (angle < rotation - 180)
                {
                    angle += 360;
                }

                angle = Mathf.Clamp(angle, rotation - clipped.ArcDegrees / 2f,
                                           rotation + clipped.ArcDegrees / 2f);

                localHitPoint.x = Mathf.Sin(angle * Mathf.Deg2Rad) * Cylinder.Radius;
                localHitPoint.z = Mathf.Cos(angle * Mathf.Deg2Rad) * Cylinder.Radius;
                nearestPointOnCenterAxis = new Vector3(0f, localHitPoint.y, 0f);
            }
            else
            {
                nearestPointOnCenterAxis = new Vector3(0f, localHitPoint.y, 0f);
                float distanceFromCenterAxis = Vector3.Distance(localHitPoint,
                                                                nearestPointOnCenterAxis);
                localHitPoint = Vector3.MoveTowards(localHitPoint,
                                                 nearestPointOnCenterAxis,
                                                 distanceFromCenterAxis - Cylinder.Radius);
            }

            bool isOutside = (localPoint - nearestPointOnCenterAxis).magnitude > Cylinder.Radius;
            Vector3 normal = (nearestPointOnCenterAxis - localHitPoint).normalized;
            switch (_cylinderSurface.Facing)
            {
                default:
                case CylinderSurface.NormalFacing.Any:
                    normal = isOutside ? -normal : normal;
                    break;
                case CylinderSurface.NormalFacing.In:
                    break;
                case CylinderSurface.NormalFacing.Out:
                    normal = -normal;
                    break;
            }

            hit = new SurfaceHit();
            hit.Point = Cylinder.transform.TransformPoint(localHitPoint);
            hit.Distance = Vector3.Distance(point, hit.Point);
            hit.Normal = Cylinder.transform.TransformDirection(normal).normalized;
            return maxDistance <= 0 || hit.Distance <= maxDistance;
        }

        #region Inject

        public void InjectAllClippedCylinderSurface(CylinderSurface surface,
            IEnumerable<ICylinderClipper> clippers)
        {
            InjectCylinderSurface(surface);
            InjectClippers(clippers);
        }

        public void InjectCylinderSurface(CylinderSurface surface)
        {
            _cylinderSurface = surface;
        }

        public void InjectClippers(IEnumerable<ICylinderClipper> clippers)
        {
            _clippers = new List<UnityEngine.Object>(
                clippers.Select(c => c as UnityEngine.Object));
        }

        #endregion
    }
}
