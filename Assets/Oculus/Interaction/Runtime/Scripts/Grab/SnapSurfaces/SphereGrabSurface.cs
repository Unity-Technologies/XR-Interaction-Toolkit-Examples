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

using System;
using UnityEngine;

namespace Oculus.Interaction.Grab.GrabSurfaces
{
    [Serializable]
    public class SphereGrabSurfaceData : ICloneable
    {
        public object Clone()
        {
            SphereGrabSurfaceData clone = new SphereGrabSurfaceData();
            clone.centre = this.centre;
            return clone;
        }

        public SphereGrabSurfaceData Mirror()
        {
            SphereGrabSurfaceData mirror = Clone() as SphereGrabSurfaceData;
            return mirror;
        }

        public Vector3 centre = Vector3.zero;
    }

    /// <summary>
    /// Specifies an entire sphere around an object in which the grip point is valid.
    /// One of the main advantages of spheres is that the rotation of the hand pose does
    /// not really matters, as it will always fit the surface correctly.
    /// </summary>
    [Serializable]
    public class SphereGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        protected SphereGrabSurfaceData _data = new SphereGrabSurfaceData();

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the grab surface")]
        private Transform _relativeTo;

        private Pose RelativePose => PoseUtils.DeltaScaled(_relativeTo, this.transform);

        /// <summary>
        /// The reference pose of the surface. It defines the radius of the sphere
        /// as the point from the relative transform to the reference pose to ensure
        /// that the sphere covers this pose.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Pose in world space</returns>
        public Pose GetReferencePose(Transform relativeTo)
        {
            return PoseUtils.GlobalPoseScaled(relativeTo, RelativePose);
        }

        /// <summary>
        /// The center of the sphere in world coordinates.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Position in world space</returns>
        public Vector3 GetCentre(Transform relativeTo)
        {
            return relativeTo.TransformPoint(_data.centre);
        }
        public void SetCentre(Vector3 point, Transform relativeTo)
        {
            _data.centre = relativeTo.InverseTransformPoint(point);
        }

        /// <summary>
        /// The radius of the sphere, this is automatically calculated as the distance between
        /// the center and the original grip pose.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Distance in world space</returns>
        public float GetRadius(Transform relativeTo)
        {
            Vector3 centre = GetCentre(relativeTo);
            Pose referencePose = GetReferencePose(relativeTo);
            return Vector3.Distance(centre, referencePose.position);
        }

        /// <summary>
        /// The direction of the sphere, measured from the center to the original grip position.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetDirection(Transform relativeTo)
        {
            Vector3 centre = GetCentre(relativeTo);
            Pose referencePose = GetReferencePose(relativeTo);
            return (referencePose.position - centre).normalized;
        }

        #region editor events
        protected virtual void Reset()
        {
            _relativeTo = this.GetComponentInParent<IRelativeToRef>()?.RelativeTo;
        }
        #endregion

        protected virtual void Start()
        {
            this.AssertField(_data, nameof(_data));
            this.AssertField(_relativeTo, nameof(_relativeTo));
        }

        public Pose MirrorPose(in Pose pose, Transform relativeTo)
        {
            Vector3 normal = Quaternion.Inverse(relativeTo.rotation) * GetDirection(relativeTo);
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            return pose.MirrorPoseRotation(normal, tangent);
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, out Pose bestPose, Transform relativeTo)
        {
            Vector3 centre = GetCentre(relativeTo);
            Vector3 projection = Vector3.Project(centre - targetRay.origin, targetRay.direction);
            Vector3 nearestCentre = targetRay.origin + projection;
            float radius = GetRadius(relativeTo);
            float distanceToSurface = Mathf.Max(Vector3.Distance(centre, nearestCentre) - radius);
            if (distanceToSurface < radius)
            {
                float adjustedDistance = Mathf.Sqrt(radius * radius - distanceToSurface * distanceToSurface);
                nearestCentre -= targetRay.direction * adjustedDistance;
            }

            Pose recordedPose = GetReferencePose(relativeTo);
            Vector3 surfacePoint = NearestPointInSurface(nearestCentre, relativeTo);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, relativeTo);
            return true;
        }

        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier, Transform relativeTo)
        {
            return GrabPoseHelper.CalculateBestPoseAtSurface(targetPose, out bestPose,
                scoringModifier, relativeTo,
                MinimalTranslationPoseAtSurface,
                MinimalRotationPoseAtSurface);
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            SphereGrabSurface surface = gameObject.AddComponent<SphereGrabSurface>();
            surface._data = _data.Mirror();
            return surface;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            SphereGrabSurface surface = gameObject.AddComponent<SphereGrabSurface>();
            surface._data = _data;
            return surface;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition, Transform relativeTo)
        {
            Vector3 centre = GetCentre(relativeTo);
            Vector3 direction = (targetPosition - centre).normalized;
            float radius = GetRadius(relativeTo);
            return centre + direction * radius;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Vector3 centre = GetCentre(relativeTo);
            Pose referencePose = GetReferencePose(relativeTo);
            float radius = GetRadius(relativeTo);
            Quaternion rotCorrection = userPose.rotation * Quaternion.Inverse(referencePose.rotation);
            Vector3 correctedDir = rotCorrection * GetDirection(relativeTo);
            Vector3 surfacePoint = NearestPointInSurface(centre + correctedDir * radius, relativeTo);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, referencePose.rotation, userPose.rotation, relativeTo);

            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Pose referencePose = GetReferencePose(relativeTo);
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;
            Vector3 surfacePoint = NearestPointInSurface(desiredPos, relativeTo);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, baseRot, userPose.rotation, relativeTo);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotationAtPoint(Vector3 surfacePoint,
            Quaternion baseRot, Quaternion desiredRotation,
            Transform relativeTo)
        {
            Vector3 desiredDirection = (surfacePoint - GetCentre(relativeTo)).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(GetDirection(relativeTo), desiredDirection) * baseRot;
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, desiredDirection).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(desiredRotation * Vector3.forward, desiredDirection).normalized;
            Quaternion rotCorrection = Quaternion.FromToRotation(targetProjected, desiredProjected);
            return rotCorrection * targetRotation;
        }

        #region Inject

        public void InjectAllSphereSurface(SphereGrabSurfaceData data, Transform relativeTo)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
        }

        public void InjectData(SphereGrabSurfaceData data)
        {
            _data = data;
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }
        #endregion
    }
}
