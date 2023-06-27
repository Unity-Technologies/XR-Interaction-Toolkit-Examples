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

using UnityEngine.Assertions;
using UnityEngine;

namespace Oculus.Interaction.Surfaces
{
    public class CylinderSurface : MonoBehaviour, ISurface, IBounds
    {
        public enum NormalFacing
        {
            /// <summary>
            /// Raycast hit will register on the outside or inside of the cylinder,
            /// whichever is hit first.
            /// </summary>
            Any,

            /// <summary>
            /// Raycasts will pass through the outside of the cylinder and hit the inside wall.
            /// </summary>
            In,

            /// <summary>
            /// Raycast against the outside wall of the cylinder.
            /// In this mode, raycasts with an origin inside the cylinder will always fail.
            /// </summary>
            Out,
        }

        [Tooltip("The cylinder that will drive this surface.")]
        [SerializeField]
        private Cylinder _cylinder;

        [Tooltip("The normal facing of the surface. Hits will be " +
            "registered either on the outer or inner face of the cylinder " +
            "depending on this value.")]
        [SerializeField]
        private NormalFacing _facing = NormalFacing.Out;

        [Tooltip("The height of the cylinder. If zero or " +
            "negative, height will be infinite.")]
        [SerializeField]
        private float _height = 1f;

        public bool IsValid => _cylinder != null && Radius > 0;

        public float Radius => _cylinder.Radius;

        public Cylinder Cylinder => _cylinder;

        public Transform Transform => _cylinder.transform;


        public Bounds Bounds
        {
            get
            {
                float maxScale = Mathf.Max(Transform.lossyScale.x, 
                    Mathf.Max(Transform.lossyScale.y, Transform.lossyScale.z));
                float maxSize = maxScale * (Height + Radius);
                return new Bounds(Transform.position, 
                    new Vector3(maxSize, maxSize, maxSize));
            }
        }

        public NormalFacing Facing
        {
            get => _facing;
            set => _facing = value;
        }

        public float Height
        {
            get => _height;
            set => _height = value;
        }

        protected bool _started = false;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Cylinder, nameof(Cylinder));
            this.EndStart(ref _started);
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();

            if (!IsValid)
            {
                return false;
            }

            Vector3 localPoint = _cylinder.transform.InverseTransformPoint(point);
            Vector3 clampedOrigin = localPoint;

            if (_height > 0)
            {
                clampedOrigin.y = Mathf.Clamp(clampedOrigin.y, -_height / 2f, _height / 2f);
            }

            Vector3 nearestPointOnCenterAxis = Vector3.Project(clampedOrigin, Vector3.up);

            Vector3 direction = (clampedOrigin == nearestPointOnCenterAxis) ?
                Vector3.forward : (clampedOrigin - nearestPointOnCenterAxis).normalized;

            bool isOutside = (clampedOrigin - nearestPointOnCenterAxis).magnitude > Radius;
            Vector3 hitPoint = nearestPointOnCenterAxis + direction * Radius;
            float hitDistance = Vector3.Distance(localPoint, hitPoint);

            if (maxDistance > 0 && TransformScale(hitDistance) > maxDistance)
            {
                return false;
            }

            Vector3 normal;
            switch (_facing)
            {
                default:
                case NormalFacing.Any:
                    normal = isOutside ? direction : -direction;
                    break;
                case NormalFacing.In:
                    normal = -direction;
                    break;
                case NormalFacing.Out:
                    normal = direction;
                    break;
            }

            hit.Point = _cylinder.transform.TransformPoint(nearestPointOnCenterAxis + direction * Radius);
            hit.Normal = _cylinder.transform.TransformDirection(normal);
            hit.Distance = TransformScale(hitDistance);

            return true;
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            // Flatten to 2D and find intersection point with circle in local space,
            // then convert back into 3D world space

            hit = new SurfaceHit();

            if (!IsValid)
            {
                return false;
            }

            Ray localRay3D = new Ray(_cylinder.transform.InverseTransformPoint(ray.origin),
                                     _cylinder.transform.InverseTransformDirection(ray.direction).normalized);

            Ray localRay2D = new Ray(CancelY(localRay3D.origin), CancelY(localRay3D.direction).normalized);

            Vector3 originToCenter2D = -localRay2D.origin;
            Vector3 projPoint2D = localRay2D.origin + Vector3.Project(originToCenter2D, localRay2D.direction);

            float magDir2D = Vector3.Magnitude(CancelY(localRay3D.direction));
            float magProj = Vector3.Magnitude(projPoint2D);

            bool isOutside = originToCenter2D.magnitude > Radius;

            NormalFacing effectiveFacing = _facing == NormalFacing.Any && !isOutside ? NormalFacing.In : _facing;

            bool hasMissed = magProj > Radius || // Aiming toward but missing
                             Mathf.Approximately(magDir2D, 0f) || // Aiming up or down
                             (isOutside && Vector3.Dot(originToCenter2D, localRay2D.direction) < 0) || // Aiming away
                             (!isOutside && effectiveFacing == NormalFacing.Out); // Origin inside with normals out

            if (hasMissed)
            {
                return false;
            }

            float projToWall = Mathf.Sqrt(Mathf.Pow(Radius, 2) - Mathf.Pow(magProj, 2));
            float tOuter = Vector3.Distance(localRay2D.origin, projPoint2D - localRay2D.direction * projToWall) / magDir2D;
            float tInner = Vector3.Distance(localRay2D.origin, projPoint2D + localRay2D.direction * projToWall) / magDir2D;

            Vector3 localHitpointOuter = localRay3D.GetPoint(tOuter);
            Vector3 localHitpointInner = localRay3D.GetPoint(tInner);

            bool hasOuter = (maxDistance <= 0 || TransformScale(tOuter) <= maxDistance) &&
                            (_height <= 0 || Mathf.Abs(localHitpointOuter.y) <= _height / 2f);

            bool hasInner = (maxDistance <= 0 || TransformScale(tInner) <= maxDistance) &&
                            (_height <= 0 || Mathf.Abs(localHitpointInner.y) <= _height / 2f);

            if (effectiveFacing != NormalFacing.In && hasOuter)
            {
                hit.Point = _cylinder.transform.TransformPoint(localHitpointOuter);
                hit.Normal = _cylinder.transform.TransformDirection(CancelY(localHitpointOuter).normalized);
                hit.Distance = TransformScale(tOuter);
            }
            else if (hasInner)
            {
                hit.Point = _cylinder.transform.TransformPoint(localHitpointInner);
                hit.Normal = _cylinder.transform.TransformDirection(CancelY(-localHitpointInner).normalized);
                hit.Distance = TransformScale(tInner);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Local to world transformation of a float, assuming uniform scale.
        /// </summary>
        private float TransformScale(float val)
        {
            return val * _cylinder.transform.lossyScale.x;
        }

        /// <summary>
        /// Cancel the Y component of a vector
        /// </summary>
        private static Vector3 CancelY(in Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }

        #region Inject

        public void InjectAllCylinderSurface(NormalFacing facing,
                                             Cylinder cylinder,
                                             float height)
        {
            InjectNormalFacing(facing);
            InjectCylinder(cylinder);
            InjectHeight(height);
        }

        public void InjectNormalFacing(NormalFacing facing)
        {
            _facing = facing;
        }

        public void InjectCylinder(Cylinder cylinder)
        {
            _cylinder = cylinder;
        }

        public void InjectHeight(float height)
        {
            _height = height;
        }

        #endregion
    }
}
