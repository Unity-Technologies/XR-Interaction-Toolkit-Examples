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

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that rotates the target about an axis.
    /// Updates apply relative rotational changes of a GrabPoint about an axis.
    /// The axis is defined by a pivot transform: a world position and up vector.
    /// </summary>
    public class OneGrabRotateTransformer : MonoBehaviour, ITransformer
    {
        public enum Axis
        {
            Right = 0,
            Up = 1,
            Forward = 2
        }

        [SerializeField, Optional]
        private Transform _pivotTransform = null;

        public Transform Pivot => _pivotTransform != null ? _pivotTransform : transform;

        [SerializeField]
        private Axis _rotationAxis = Axis.Up;

        public Axis RotationAxis => _rotationAxis;

        [Serializable]
        public class OneGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        [SerializeField]
        private OneGrabRotateConstraints _constraints =
            new OneGrabRotateConstraints()
        {
            MinAngle = new FloatConstraint(),
            MaxAngle = new FloatConstraint()
        };

        public OneGrabRotateConstraints Constraints
        {
            get
            {
                return _constraints;
            }

            set
            {
                _constraints = value;
            }
        }

        private float _relativeAngle = 0.0f;
        private float _constrainedRelativeAngle = 0.0f;

        private IGrabbable _grabbable;
        private Vector3 _grabPositionInPivotSpace;
        private Pose _transformPoseInPivotSpace;

        private Pose _worldPivotPose;
        private Vector3 _previousVectorInPivotSpace;

        private Quaternion _localRotation;
        private float _startAngle = 0;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public Pose ComputeWorldPivotPose()
        {
            if (_pivotTransform != null)
            {
                return _pivotTransform.GetPose();
            }

            var targetTransform = _grabbable.Transform;

            Vector3 worldPosition = targetTransform.position;
            Quaternion worldRotation = targetTransform.parent != null
                ? targetTransform.parent.rotation * _localRotation
                : _localRotation;

            return new Pose(worldPosition, worldRotation);
        }

        public void BeginTransform()
        {
            var grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;

            if (_pivotTransform == null)
            {
                _localRotation = targetTransform.localRotation;
            }

            Vector3 localAxis = Vector3.zero;
            localAxis[(int)_rotationAxis] = 1f;

            _worldPivotPose = ComputeWorldPivotPose();
            Vector3 rotationAxis = _worldPivotPose.rotation * localAxis;

            Quaternion inverseRotation = Quaternion.Inverse(_worldPivotPose.rotation);

            Vector3 grabDelta = grabPoint.position - _worldPivotPose.position;
            // The initial delta must be non-zero between the pivot and grab location for rotation
            if (Mathf.Abs(grabDelta.magnitude) < 0.001f)
            {
                Vector3 localAxisNext = Vector3.zero;
                localAxisNext[((int)_rotationAxis + 1) % 3] = 0.001f;
                grabDelta = _worldPivotPose.rotation * localAxisNext;
            }

            _grabPositionInPivotSpace =
                inverseRotation * grabDelta;

            Vector3 worldPositionDelta =
                inverseRotation * (targetTransform.position - _worldPivotPose.position);

            Quaternion worldRotationDelta = inverseRotation * targetTransform.rotation;
            _transformPoseInPivotSpace = new Pose(worldPositionDelta, worldRotationDelta);

            Vector3 initialOffset = _worldPivotPose.rotation * _grabPositionInPivotSpace;
            Vector3 initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);
            _previousVectorInPivotSpace = Quaternion.Inverse(_worldPivotPose.rotation) * initialVector;

            _startAngle = _constrainedRelativeAngle;
            _relativeAngle = _startAngle;

            float parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            _transformPoseInPivotSpace.position /= parentScale;
        }

        public void UpdateTransform()
        {
            var grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;

            Vector3 localAxis = Vector3.zero;
            localAxis[(int)_rotationAxis] = 1f;
            _worldPivotPose = ComputeWorldPivotPose();
            Vector3 rotationAxis = _worldPivotPose.rotation * localAxis;

            // Project our positional offsets onto a plane with normal equal to the rotation axis
            Vector3 targetOffset = grabPoint.position - _worldPivotPose.position;
            Vector3 targetVector = Vector3.ProjectOnPlane(targetOffset, rotationAxis);

            Vector3 previousVectorInWorldSpace =
                _worldPivotPose.rotation * _previousVectorInPivotSpace;

            // update previous
            _previousVectorInPivotSpace = Quaternion.Inverse(_worldPivotPose.rotation) * targetVector;

            float signedAngle =
                Vector3.SignedAngle(previousVectorInWorldSpace, targetVector, rotationAxis);

            _relativeAngle += signedAngle;

            _constrainedRelativeAngle = _relativeAngle;
            if (Constraints.MinAngle.Constrain)
            {
                _constrainedRelativeAngle = Mathf.Max(_constrainedRelativeAngle, Constraints.MinAngle.Value);
            }
            if (Constraints.MaxAngle.Constrain)
            {
                _constrainedRelativeAngle = Mathf.Min(_constrainedRelativeAngle, Constraints.MaxAngle.Value);
            }

            Quaternion deltaRotation = Quaternion.AngleAxis(_constrainedRelativeAngle - _startAngle, rotationAxis);

            float parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            Pose transformDeltaInWorldSpace =
                new Pose(
                    _worldPivotPose.rotation * (parentScale * _transformPoseInPivotSpace.position),
                    _worldPivotPose.rotation * _transformPoseInPivotSpace.rotation);

            Pose transformDeltaRotated = new Pose(
                deltaRotation * transformDeltaInWorldSpace.position,
                deltaRotation * transformDeltaInWorldSpace.rotation);

            targetTransform.position = _worldPivotPose.position + transformDeltaRotated.position;
            targetTransform.rotation = transformDeltaRotated.rotation;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalPivotTransform(Transform pivotTransform)
        {
            _pivotTransform = pivotTransform;
        }

        public void InjectOptionalRotationAxis(Axis rotationAxis)
        {
            _rotationAxis = rotationAxis;
        }

        public void InjectOptionalConstraints(OneGrabRotateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
