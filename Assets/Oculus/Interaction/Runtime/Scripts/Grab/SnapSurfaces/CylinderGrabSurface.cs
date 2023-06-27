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
using UnityEngine.Serialization;

namespace Oculus.Interaction.Grab.GrabSurfaces
{
    [Serializable]
    public class CylinderSurfaceData : ICloneable
    {
        public object Clone()
        {
            CylinderSurfaceData clone = new CylinderSurfaceData();
            clone.startPoint = this.startPoint;
            clone.endPoint = this.endPoint;
            clone.arcOffset = this.arcOffset;
            clone.arcLength = this.arcLength;
            return clone;
        }

        public CylinderSurfaceData Mirror()
        {
            CylinderSurfaceData mirror = Clone() as CylinderSurfaceData;
            return mirror;
        }

        public Vector3 startPoint = new Vector3(0f, 0.1f, 0f);
        public Vector3 endPoint = new Vector3(0f, -0.1f, 0f);

        [Range(0f, 360f)]
        public float arcOffset = 0f;
        [Range(0f, 360f)]
        [FormerlySerializedAs("angle")]
        public float arcLength = 360f;
    }

    /// <summary>
    /// This type of surface defines a cylinder in which the grip pose is valid around an object.
    /// An angle and offset can be used to constrain the cylinder and not use a full circle.
    /// The radius is automatically specified as the distance from the axis of the cylinder to the original grip position.
    /// </summary>
    [Serializable]
    public class CylinderGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        protected CylinderSurfaceData _data = new CylinderSurfaceData();

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the grab surface")]
        private Transform _relativeTo;

        private Pose RelativePose => PoseUtils.DeltaScaled(_relativeTo, this.transform);
        private const float Epsilon = 0.000001f;
        /// <summary>
        /// The reference pose of the surface. It defines the radius of the cylinder
        /// as the point from the relative transform to the reference pose to ensure
        /// that the cylinder covers this pose.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Pose in world space</returns>
        public Pose GetReferencePose(Transform relativeTo)
        {
            return PoseUtils.GlobalPoseScaled(relativeTo, RelativePose);
        }

        /// <summary>
        /// Degrees from the starting radius from which the arc section starts
        /// </summary>
        public float ArcOffset
        {
            get
            {
                return _data.arcOffset;
            }
            set
            {
                if (value != 0 && value % 360f == 0)
                {
                    _data.arcOffset = 360f;
                }
                else
                {
                    _data.arcOffset = Mathf.Repeat(value, 360f);
                }
            }
        }

        /// <summary>
        /// The maximum angle for the surface of the cylinder, starting from the ArcOffset.
        /// To invert the direction of the angle, swap the caps order.
        /// </summary>
        public float ArcLength
        {
            get
            {
                return _data.arcLength;
            }
            set
            {
                if (value != 0 && value % 360f == 0)
                {
                    _data.arcLength = 360f;
                }
                else
                {
                    _data.arcLength = Mathf.Repeat(value, 360f);
                }
            }
        }

        /// <summary>
        /// The direction of the main radius of the cylinder. This is the
        /// radius from the center of the cylinder to the reference position.
        /// </summary>
        private Vector3 LocalPerpendicularDir
        {
            get
            {
                return Vector3.ProjectOnPlane(RelativePose.position - _data.startPoint, LocalDirection).normalized;
            }
        }

        /// <summary>
        /// The direction of the central axis of the cylinder in local space.
        /// </summary>
        private Vector3 LocalDirection
        {
            get
            {
                Vector3 dir = (_data.endPoint - _data.startPoint);
                if (dir.sqrMagnitude <= Epsilon)
                {
                    return Vector3.up;
                }
                return dir.normalized;
            }
        }

        /// <summary>
        /// Direction from the axis of the cylinder to the original grip position.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetPerpendicularDir(Transform relativeTo)
        {
            return relativeTo.TransformDirection(LocalPerpendicularDir);
        }

        /// <summary>
        /// Direction from the axis of the cylinder to the minimum angle allowance.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetStartArcDir(Transform relativeTo)
        {
            Vector3 localStartArcDir = Quaternion.AngleAxis(ArcOffset, LocalDirection) * LocalPerpendicularDir;
            return relativeTo.TransformDirection(localStartArcDir);
        }

        /// <summary>
        /// Direction from the axis of the cylinder to the maximum angle allowance.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetEndArcDir(Transform relativeTo)
        {
            Vector3 localEndArcDir = Quaternion.AngleAxis(ArcLength, LocalDirection) *
                Quaternion.AngleAxis(ArcOffset, LocalDirection) * LocalPerpendicularDir;
            return relativeTo.TransformDirection(localEndArcDir);
        }

        /// <summary>
        /// Base cap of the cylinder, in world coordinates.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Position in world space</returns>
        public Vector3 GetStartPoint(Transform relativeTo)
        {
            return relativeTo.TransformPoint(_data.startPoint);
        }
        public void SetStartPoint(Vector3 point, Transform relativeTo)
        {
            _data.startPoint = relativeTo.InverseTransformPoint(point);
        }

        /// <summary>
        /// End cap of the cylinder, in world coordinates.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Position in world space</returns>
        public Vector3 GetEndPoint(Transform relativeTo)
        {
            return relativeTo.TransformPoint(_data.endPoint);
        }
        public void SetEndPoint(Vector3 point, Transform relativeTo)
        {
            _data.endPoint = relativeTo.InverseTransformPoint(point);
        }

        /// <summary>
        /// The generated radius of the cylinder.
        /// Represents the distance from the axis of the cylinder to the original grip position.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Distance in world space</returns>
        public float GetRadius(Transform relativeTo)
        {
            Vector3 start = GetStartPoint(relativeTo);
            Pose referencePose = GetReferencePose(relativeTo);
            Vector3 direction = GetDirection(relativeTo);
            Vector3 projectedPoint = start + Vector3.Project(referencePose.position - start, direction);
            return Vector3.Distance(projectedPoint, referencePose.position);
        }

        /// <summary>
        /// Direction of the cylinder, from the start cap to the end cap.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetDirection(Transform relativeTo)
        {
            return relativeTo.TransformDirection(LocalDirection);
        }

        /// <summary>
        /// Length of the cylinder, from the start cap to the end cap.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Distance in world space</returns>
        private float GetHeight(Transform relativeTo)
        {
            Vector3 start = GetStartPoint(relativeTo);
            Vector3 end = GetEndPoint(relativeTo);
            return Vector3.Distance(start, end);
        }

        /// <summary>
        /// The rotation of the central axis of the cylinder.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Rotation in world space</returns>
        private Quaternion GetRotation(Transform relativeTo)
        {
            if (_data.startPoint == _data.endPoint)
            {
                return relativeTo.rotation;
            }
            return relativeTo.rotation * Quaternion.LookRotation(LocalPerpendicularDir, LocalDirection);
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
            Vector3 normal = Quaternion.Inverse(relativeTo.rotation) * GetPerpendicularDir(relativeTo);
            Vector3 tangent = Quaternion.Inverse(relativeTo.rotation) * GetDirection(relativeTo);

            return pose.MirrorPoseRotation(normal, tangent);
        }

        private Vector3 PointAltitude(Vector3 point, Transform relativeTo)
        {
            Vector3 start = GetStartPoint(relativeTo);
            Vector3 direction = GetDirection(relativeTo);
            Vector3 projectedPoint = start + Vector3.Project(point - start, direction);
            return projectedPoint;
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
            CylinderGrabSurface surface = gameObject.AddComponent<CylinderGrabSurface>();
            surface._data = _data.Mirror();
            return surface;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            CylinderGrabSurface surface = gameObject.AddComponent<CylinderGrabSurface>();
            surface._data = _data.Clone() as CylinderSurfaceData;
            return surface;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition, Transform relativeTo)
        {
            Vector3 start = GetStartPoint(relativeTo);
            Vector3 dir = GetDirection(relativeTo);
            Vector3 projectedVector = Vector3.Project(targetPosition - start, dir);
            float height = GetHeight(relativeTo);
            if (projectedVector.magnitude > height)
            {
                projectedVector = projectedVector.normalized * height;
            }
            if (Vector3.Dot(projectedVector, dir) < 0f)
            {
                projectedVector = Vector3.zero;
            }

            Vector3 projectedPoint = start + projectedVector;
            Vector3 targetDirection = Vector3.ProjectOnPlane((targetPosition - projectedPoint), dir).normalized;

            Vector3 startArcDir = GetStartArcDir(relativeTo);
            float desiredAngle = Mathf.Repeat(Vector3.SignedAngle(startArcDir, targetDirection, dir), 360f);
            if (desiredAngle > ArcLength)
            {
                if (Mathf.Abs(desiredAngle - ArcLength) >= Mathf.Abs(360f - desiredAngle))
                {
                    targetDirection = startArcDir;
                }
                else
                {
                    targetDirection = GetEndArcDir(relativeTo);
                }
            }
            Vector3 surfacePoint = projectedPoint + targetDirection * GetRadius(relativeTo);
            return surfacePoint;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, out Pose bestPose,
            Transform relativeTo)
        {
            Pose recordedPose = GetReferencePose(relativeTo);
            Vector3 start = GetStartPoint(relativeTo);
            Vector3 direction = GetDirection(relativeTo);
            Vector3 lineToCylinder = start - targetRay.origin;

            float perpendiculiarity = Vector3.Dot(targetRay.direction, direction);
            float rayToLineDiff = Vector3.Dot(lineToCylinder, targetRay.direction);
            float cylinderToLineDiff = Vector3.Dot(lineToCylinder, direction);

            float determinant = 1f / (perpendiculiarity * perpendiculiarity - 1f);

            float lineOffset = (perpendiculiarity * cylinderToLineDiff - rayToLineDiff) * determinant;
            float cylinderOffset = (cylinderToLineDiff - perpendiculiarity * rayToLineDiff) * determinant;

            float radius = GetRadius(relativeTo);
            Vector3 pointInLine = targetRay.origin + targetRay.direction * lineOffset;
            Vector3 pointInCylinder = start + direction * cylinderOffset;
            float distanceToSurface = Mathf.Max(Vector3.Distance(pointInCylinder, pointInLine) - radius);
            if (distanceToSurface < radius)
            {
                float adjustedDistance = Mathf.Sqrt(radius * radius - distanceToSurface * distanceToSurface);
                pointInLine -= targetRay.direction * adjustedDistance;
            }
            Vector3 surfacePoint = NearestPointInSurface(pointInLine, relativeTo);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, relativeTo);

            return true;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Pose referencePose = GetReferencePose(relativeTo);
            Vector3 direction = GetDirection(relativeTo);
            Quaternion rotation = GetRotation(relativeTo);
            float radius = GetRadius(relativeTo);
            Vector3 desiredPos = userPose.position;
            Quaternion desiredRot = userPose.rotation;
            Quaternion baseRot = referencePose.rotation;
            Quaternion rotDif = (desiredRot) * Quaternion.Inverse(baseRot);
            Vector3 desiredDirection = (rotDif * rotation) * Vector3.forward;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(desiredDirection, direction).normalized;
            Vector3 altitudePoint = PointAltitude(desiredPos, relativeTo);
            Vector3 surfacePoint = NearestPointInSurface(altitudePoint + projectedDirection * radius, relativeTo);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint, relativeTo) * baseRot;
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Pose referencePose = GetReferencePose(relativeTo);
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;
            Vector3 surfacePoint = NearestPointInSurface(desiredPos, relativeTo);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint, relativeTo) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion CalculateRotationOffset(Vector3 surfacePoint, Transform relativeTo)
        {
            Vector3 start = GetStartPoint(relativeTo);
            Vector3 direction = GetDirection(relativeTo);
            Vector3 referenceDir = GetPerpendicularDir(relativeTo);
            Vector3 recordedDirection = Vector3.ProjectOnPlane(referenceDir, direction);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(surfacePoint - start, direction);
            return Quaternion.FromToRotation(recordedDirection, desiredDirection);
        }

        #region Inject

        public void InjectAllCylinderSurface(CylinderSurfaceData data,
            Transform relativeTo)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
        }

        public void InjectData(CylinderSurfaceData data)
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
