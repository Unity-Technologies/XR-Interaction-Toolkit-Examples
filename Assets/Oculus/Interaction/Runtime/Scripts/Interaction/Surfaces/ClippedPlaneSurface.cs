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
using System.Collections.Generic;
using System.Linq;

namespace Oculus.Interaction.Surfaces
{
    public class ClippedPlaneSurface : MonoBehaviour, IClippedSurface<IBoundsClipper>
    {
        private static readonly Bounds InfiniteBounds = new Bounds(Vector3.zero,
            Vector3.one * float.PositiveInfinity);

        private static readonly Bounds PlaneBounds = new Bounds(Vector3.zero,
            new Vector3(float.PositiveInfinity, float.PositiveInfinity, Vector3.kEpsilon));

        [Tooltip("The Plane Surface to be clipped.")]
        [SerializeField]
        private PlaneSurface _planeSurface;

        [Tooltip("The clippers that will be used to clip the Plane Surface.")]
        [SerializeField, Interface(typeof(IBoundsClipper))]
        private List<UnityEngine.Object> _clippers;

        public ISurface BackingSurface => _planeSurface;

        public Transform Transform => _planeSurface.Transform;

        public IEnumerable<IBoundsClipper> GetClippers()
        {
            foreach (var clipper in _clippers)
            {
                yield return clipper as IBoundsClipper;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(_planeSurface, nameof(_planeSurface));
            this.AssertCollectionItems(_clippers, nameof(_clippers));
        }

        /// <summary>
        /// Clip a provided Bounds using
        /// <see cref="IBoundsClipper"/>s
        /// </summary>
        /// <param name="bounds">The bounding box to clip</param>
        /// <param name="clipped">The clipped result</param>
        /// <returns>True if resulting bounds are valid,
        /// false if resulting bounds are fully clipped.</returns>
        public bool ClipBounds(in Bounds bounds, out Bounds clipped)
        {
            clipped = bounds;

            foreach (var clipperMono in _clippers)
            {
                IBoundsClipper clipper = clipperMono as IBoundsClipper;
                if (clipper == null ||
                    !clipper.GetLocalBounds(Transform, out Bounds clipTo))
                {
                    continue;
                }

                if (!clipped.Clip(clipTo, out clipped))
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 ClampPoint(in Vector3 point, in Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 localPoint = Transform.InverseTransformPoint(point);

            Vector3 clamped = new Vector3(
                Mathf.Clamp(localPoint.x, min.x, max.x),
                Mathf.Clamp(localPoint.y, min.y, max.y),
                Mathf.Clamp(localPoint.z, min.z, max.z));

            return Transform.TransformPoint(clamped);
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            if (_planeSurface.ClosestSurfacePoint(point, out hit, maxDistance) &&
                ClipBounds(PlaneBounds, out Bounds clippedPlane))
            {
                hit.Point = ClampPoint(hit.Point, clippedPlane);
                hit.Distance = Vector3.Distance(point, hit.Point);
                return maxDistance <= 0 || hit.Distance <= maxDistance;
            }
            return false;
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            return BackingSurface.Raycast(ray, out hit, maxDistance) &&
                   ClipBounds(InfiniteBounds, out Bounds clipBounds) &&
                   clipBounds.size != Vector3.zero &&
                   clipBounds.Contains(Transform.InverseTransformPoint(hit.Point));
        }

        #region Inject

        public void InjectAllClippedPlaneSurface(
            PlaneSurface planeSurface,
            IEnumerable<IBoundsClipper> clippers)
        {
            InjectPlaneSurface(planeSurface);
            InjectClippers(clippers);
        }

        public void InjectPlaneSurface(PlaneSurface planeSurface)
        {
            _planeSurface = planeSurface;
        }

        public void InjectClippers(IEnumerable<IBoundsClipper> clippers)
        {
            _clippers = new List<UnityEngine.Object>(
                clippers.Select(c => c as UnityEngine.Object));
        }

        #endregion
    }
}
