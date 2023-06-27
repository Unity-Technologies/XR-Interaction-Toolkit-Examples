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
    public class BoxGrabSurfaceData : ICloneable
    {
        public object Clone()
        {
            BoxGrabSurfaceData clone = new BoxGrabSurfaceData();
            clone.widthOffset = this.widthOffset;
            clone.snapOffset = this.snapOffset;
            clone.size = this.size;
            clone.eulerAngles = this.eulerAngles;
            return clone;
        }

        public BoxGrabSurfaceData Mirror()
        {
            BoxGrabSurfaceData mirror = Clone() as BoxGrabSurfaceData;
            mirror.snapOffset = new Vector4(
                -mirror.snapOffset.y, -mirror.snapOffset.x,
                -mirror.snapOffset.w, -mirror.snapOffset.z);

            return mirror;
        }

        [Range(0f, 1f)]
        public float widthOffset = 0.5f;
        public Vector4 snapOffset;
        public Vector3 size = new Vector3(0.1f, 0f, 0.1f);
        public Vector3 eulerAngles;
    }

    /// <summary>
    /// This GrabSurface defines a Rectangle around which the grip point is valid.
    /// Since the grip point might be offset from the fingers, a valid range for each opposite
    /// side of the rectangle can be set so the grabbing fingers are constrained to the object bounds.
    /// </summary>
    [Serializable]
    public class BoxGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        protected BoxGrabSurfaceData _data = new BoxGrabSurfaceData();

        [SerializeField]
        [Tooltip("Transform used as a reference to measure the local data of the grab surface")]
        private Transform _relativeTo;

        private Pose RelativePose => PoseUtils.DeltaScaled(_relativeTo, this.transform);

        /// <summary>
        /// The origin pose of the surface. This is the point from which
        /// the base of the box must start.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Pose in world space</returns>
        public Pose GetReferencePose(Transform relativeTo)
        {
            return PoseUtils.GlobalPoseScaled(relativeTo, RelativePose);
        }

        /// <summary>
        /// The lateral displacement of the grip point in the main side.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Lateral displacement in world space</returns>
        public float GetWidthOffset(Transform relativeTo)
        {
            return _data.widthOffset * relativeTo.lossyScale.x;
        }
        public void SetWidthOffset(float widthOffset, Transform relativeTo)
        {
            _data.widthOffset = widthOffset / relativeTo.lossyScale.x;
        }

        /// <summary>
        /// The range at which the sides are constrained.
        /// X,Y for the back and forward sides range.
        /// Z,W for the left and right sides range.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Offsets in world space</returns>
        public Vector4 GetSnapOffset(Transform relativeTo)
        {
            return _data.snapOffset * relativeTo.lossyScale.x;
        }
        public void SetSnapOffset(Vector4 snapOffset, Transform relativeTo)
        {
            _data.snapOffset = snapOffset / relativeTo.lossyScale.x;
        }

        /// <summary>
        /// The size of the rectangle. Y is ignored.
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Size in world space</returns>
        public Vector3 GetSize(Transform relativeTo)
        {
            return _data.size * relativeTo.lossyScale.x;
        }
        public void SetSize(Vector3 size, Transform relativeTo)
        {
            _data.size = size / relativeTo.lossyScale.x;
        }

        /// <summary>
        /// The rotation of the rectangle from the Grip point
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Rotation in world space</returns>
        public Quaternion GetRotation(Transform relativeTo)
        {
            return relativeTo.rotation * Quaternion.Euler(_data.eulerAngles);
        }
        public void SetRotation(Quaternion rotation, Transform relativeTo)
        {
            _data.eulerAngles = (Quaternion.Inverse(relativeTo.rotation) * rotation).eulerAngles;
        }

        /// <summary>
        /// The forward direction of the rectangle (based on its rotation)
        /// </summary>
        /// <param name="relativeTo">The reference transform to apply the surface to</param>
        /// <returns>Direction in world space</returns>
        public Vector3 GetDirection(Transform relativeTo)
        {
            return GetRotation(relativeTo) * Vector3.forward;
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
            Quaternion rotation = GetRotation(relativeTo);
            Vector3 normal = Quaternion.Inverse(relativeTo.rotation) * rotation * Vector3.forward;
            Vector3 tangent = Quaternion.Inverse(relativeTo.rotation) * (rotation * Vector3.up);
            return pose.MirrorPoseRotation(normal, tangent);
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            BoxGrabSurface surface = gameObject.AddComponent<BoxGrabSurface>();
            surface._data = _data.Mirror();
            return surface;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            BoxGrabSurface surface = gameObject.AddComponent<BoxGrabSurface>();
            surface._data = _data;
            return surface;
        }

        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier, Transform relativeTo)
        {
            return GrabPoseHelper.CalculateBestPoseAtSurface(targetPose, out bestPose,
                scoringModifier, relativeTo,
                MinimalTranslationPoseAtSurface, MinimalRotationPoseAtSurface);
        }

        private void CalculateCorners(out Vector3 bottomLeft, out Vector3 bottomRight, out Vector3 topLeft, out Vector3 topRight,
            Transform relativeTo)
        {
            Pose referencePose = GetReferencePose(relativeTo);
            Vector3 size = GetSize(relativeTo);
            float widthOffset = GetWidthOffset(relativeTo);
            Vector3 rightRot = GetRotation(relativeTo) * Vector3.right;
            bottomLeft = referencePose.position - rightRot * size.x * (1f - widthOffset);
            bottomRight = referencePose.position + rightRot * size.x * (widthOffset);
            Vector3 forwardOffset = GetRotation(relativeTo) * Vector3.forward * size.z;
            topLeft = bottomLeft + forwardOffset;
            topRight = bottomRight + forwardOffset;
        }

        private Vector3 ProjectOnSegment(Vector3 point, (Vector3, Vector3) segment)
        {
            Vector3 line = segment.Item2 - segment.Item1;
            Vector3 projection = Vector3.Project(point - segment.Item1, line);
            if (Vector3.Dot(projection, line) < 0f)
            {
                projection = segment.Item1;
            }
            else if (projection.magnitude > line.magnitude)
            {
                projection = segment.Item2;
            }
            else
            {
                projection += segment.Item1;
            }
            return projection;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, out Pose bestPose, Transform relativeTo)
        {
            Pose recordedPose = GetReferencePose(relativeTo);
            Plane plane = new Plane(GetRotation(relativeTo) * Vector3.up, this.transform.position);
            plane.Raycast(targetRay, out float rayDistance);
            Vector3 proximalPoint = targetRay.origin + targetRay.direction * rayDistance;

            Vector3 surfacePoint = NearestPointInSurface(proximalPoint, relativeTo);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, relativeTo);
            return true;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition, Transform relativeTo)
        {
            NearestPointAndAngleInSurface(targetPosition, out Vector3 surfacePoint, out float angle, relativeTo);
            return surfacePoint;
        }

        private void NearestPointAndAngleInSurface(Vector3 targetPosition, out Vector3 surfacePoint, out float angle, Transform relativeTo)
        {
            Quaternion rotation = GetRotation(relativeTo);
            Vector4 snappOffset = GetSnapOffset(relativeTo);
            Vector3 rightDir = rotation * Vector3.right;
            Vector3 forwardDir = rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            CalculateCorners(out bottomLeft, out bottomRight, out topLeft, out topRight, relativeTo);

            Vector3 bottomP = ProjectOnSegment(targetPosition,
                (bottomLeft + rightDir * snappOffset.y, bottomRight + rightDir * snappOffset.x));
            Vector3 topP = ProjectOnSegment(targetPosition,
                (topLeft - rightDir * snappOffset.x, topRight - rightDir * snappOffset.y));
            Vector3 leftP = ProjectOnSegment(targetPosition,
                (bottomLeft - forwardDir * snappOffset.z, topLeft - forwardDir * snappOffset.w));
            Vector3 rightP = ProjectOnSegment(targetPosition,
                (bottomRight + forwardDir * snappOffset.w, topRight + forwardDir * snappOffset.z));

            float bottomDistance = (bottomP - targetPosition).sqrMagnitude;
            float topDistance = (topP - targetPosition).sqrMagnitude;
            float leftDistance = (leftP - targetPosition).sqrMagnitude;
            float rightDistance = (rightP - targetPosition).sqrMagnitude;

            float minDistance = Mathf.Min(bottomDistance,
                Mathf.Min(topDistance,
                Mathf.Min(leftDistance, rightDistance)));
            if (bottomDistance == minDistance)
            {
                surfacePoint = bottomP;
                angle = 0f;
                return;
            }
            if (topDistance == minDistance)
            {
                surfacePoint = topP;
                angle = 180f;
                return;
            }
            if (leftDistance == minDistance)
            {
                surfacePoint = leftP;
                angle = 90f;
                return;
            }
            surfacePoint = rightP;
            angle = -90f;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Quaternion rotation = GetRotation(relativeTo);
            Pose referencePose = GetReferencePose(relativeTo);
            Vector4 snappOffset = GetSnapOffset(relativeTo);
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;
            Quaternion desiredRot = userPose.rotation;
            Vector3 up = rotation * Vector3.up;

            Quaternion bottomRot = baseRot;
            Quaternion topRot = Quaternion.AngleAxis(180f, up) * baseRot;
            Quaternion leftRot = Quaternion.AngleAxis(90f, up) * baseRot;
            Quaternion rightRot = Quaternion.AngleAxis(-90f, up) * baseRot;

            float bottomDot = RotationalScore(bottomRot, desiredRot);
            float topDot = RotationalScore(topRot, desiredRot);
            float leftDot = RotationalScore(leftRot, desiredRot);
            float rightDot = RotationalScore(rightRot, desiredRot);

            Vector3 rightDir = rotation * Vector3.right;
            Vector3 forwardDir = rotation * Vector3.forward;
            Vector3 bottomLeft, bottomRight, topLeft, topRight;
            CalculateCorners(out bottomLeft, out bottomRight, out topLeft, out topRight, relativeTo);

            float maxDot = Mathf.Max(bottomDot, Mathf.Max(topDot, Mathf.Max(leftDot, rightDot)));
            if (bottomDot == maxDot)
            {
                Vector3 projBottom = ProjectOnSegment(desiredPos, (bottomLeft + rightDir * snappOffset.y, bottomRight + rightDir * snappOffset.x));
                return new Pose(projBottom, bottomRot);
            }
            if (topDot == maxDot)
            {
                Vector3 projTop = ProjectOnSegment(desiredPos, (topLeft - rightDir * snappOffset.x, topRight - rightDir * snappOffset.y));
                return new Pose(projTop, topRot);
            }
            if (leftDot == maxDot)
            {
                Vector3 projLeft = ProjectOnSegment(desiredPos, (bottomLeft - forwardDir * snappOffset.z, topLeft - forwardDir * snappOffset.w));
                return new Pose(projLeft, leftRot);
            }
            Vector3 projRight = ProjectOnSegment(desiredPos, (bottomRight + forwardDir * snappOffset.w, topRight + forwardDir * snappOffset.z));
            return new Pose(projRight, rightRot);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, Transform relativeTo)
        {
            Pose referencePose = GetReferencePose(relativeTo);
            Quaternion rotation = GetRotation(relativeTo);
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;
            NearestPointAndAngleInSurface(desiredPos, out Vector3 surfacePoint, out float surfaceAngle, relativeTo);
            Quaternion surfaceRotation = Quaternion.AngleAxis(surfaceAngle, rotation * Vector3.up) * baseRot;
            return new Pose(surfacePoint, surfaceRotation);
        }

        private static float RotationalScore(in Quaternion from, in Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return (forwardDifference * upDifference);
        }

        #region Inject

        public void InjectAllBoxSurface(BoxGrabSurfaceData data, Transform relativeTo)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
        }

        public void InjectData(BoxGrabSurfaceData data)
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
