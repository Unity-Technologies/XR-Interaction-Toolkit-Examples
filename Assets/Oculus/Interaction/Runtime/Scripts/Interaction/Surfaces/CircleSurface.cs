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

namespace Oculus.Interaction.Surfaces
{
    public class CircleSurface : MonoBehaviour, ISurfacePatch
    {
        [Tooltip("The circle will lay upon this plane, with " +
            "the circle's center at the plane surface's origin.")]
        [SerializeField]
        private PlaneSurface _planeSurface;

        [Tooltip("The radius of the circle.")]
        [SerializeField]
        private float _radius = 0.1f;

        public Transform Transform => _planeSurface.Transform;

        public ISurface BackingSurface => _planeSurface;

        protected virtual void Start()
        {
            this.AssertField(_planeSurface, nameof(_planeSurface));
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance = 0)
        {
            hit = new SurfaceHit();
            Plane plane = new Plane(_planeSurface.Normal, Transform.position);

            if (plane.Raycast(ray, out float hitDistance))
            {
                if (maxDistance > 0 && hitDistance > maxDistance)
                {
                    return false;
                }

                Vector3 hitPointWorld = ray.GetPoint(hitDistance);
                Vector3 hitPointLocal = Transform.InverseTransformPoint(hitPointWorld);

                if (Mathf.Abs(hitPointLocal.x) > _radius ||
                    Mathf.Abs(hitPointLocal.y) > _radius)
                {
                    return false;
                }

                hit.Point = hitPointWorld;
                hit.Normal = plane.normal;
                hit.Distance = hitDistance;
                return true;
            }

            return false;
        }

        // Closest point to circle is computed by projecting point to the plane
        // the circle is on and then clamping to the circle
        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance = 0)
        {
            hit = new SurfaceHit();

            Vector3 vectorFromPlane = point - Transform.position;
            Vector3 planeNormal = _planeSurface.Normal;
            Vector3 projectedPoint = Vector3.ProjectOnPlane(vectorFromPlane, planeNormal);

            float distanceFromCenterSqr = projectedPoint.sqrMagnitude;
            float worldRadius = Transform.lossyScale.x * _radius;
            if (distanceFromCenterSqr > worldRadius * worldRadius)
            {
                projectedPoint = worldRadius * projectedPoint.normalized;
            }
            Vector3 closestPoint = projectedPoint + Transform.position;

            hit.Point = closestPoint;
            hit.Normal = -1.0f * Transform.forward;
            hit.Distance = Vector3.Distance(point, closestPoint);
            return maxDistance <= 0 || hit.Distance <= maxDistance;
        }

        #region Inject
        public void InjectAllCircleProximityField(PlaneSurface planeSurface)
        {
            InjectPlaneSurface(planeSurface);
        }

        public void InjectPlaneSurface(PlaneSurface planeSurface)
        {
            _planeSurface = planeSurface;
        }

        public void InjectOptionalRadius(float radius)
        {
            _radius = radius;
        }

        #endregion
    }
}
