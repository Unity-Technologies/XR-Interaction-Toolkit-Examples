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
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that translates, rotates and scales the target on a plane.
    /// </summary>
    public class TwoGrabPlaneTransformer : MonoBehaviour, ITransformer
    {
        [SerializeField, Optional]
        private Transform _planeTransform = null;

        private Vector3 _capturePosition;

        private Vector3 _initialLocalScale;
        private float _initialDistance;
        private float _initialScale = 1.0f;
        private float _activeScale = 1.0f;

        private Pose _previousGrabA;
        private Pose _previousGrabB;

        [Serializable]
        public class TwoGrabPlaneConstraints
        {
            public FloatConstraint MinScale;
            public FloatConstraint MaxScale;
            public FloatConstraint MinY;
            public FloatConstraint MaxY;
        }

        [SerializeField]
        private TwoGrabPlaneConstraints _constraints;

        public TwoGrabPlaneConstraints Constraints
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

        private IGrabbable _grabbable;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public void BeginTransform()
        {
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var targetTransform = _grabbable.Transform;

            // Use the centroid of our grabs as the capture plane point
            _capturePosition = targetTransform.position;

            Transform planeTransform = _planeTransform != null ? _planeTransform : targetTransform;
            Vector3 rotationAxis = planeTransform.up;

            // Project our positional offsets onto a plane with normal equal to the rotation axis
            Vector3 initialOffset = grabB.position - grabA.position;
            Vector3 initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);
            _initialDistance = initialVector.magnitude;

            _initialScale = _activeScale = targetTransform.localScale.x;
            _previousGrabA = grabA;
            _previousGrabB = grabB;
        }

        public void UpdateTransform()
        {
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var targetTransform = _grabbable.Transform;

            // Use the centroid of our grabs as the transformation center
            Vector3 initialCenter = Vector3.Lerp(_previousGrabA.position, _previousGrabB.position, 0.5f);
            Vector3 targetCenter = Vector3.Lerp(grabA.position, grabB.position, 0.5f);

            Transform planeTransform = _planeTransform != null ? _planeTransform : targetTransform;
            Vector3 rotationAxis = planeTransform.up;

            // Project our positional offsets onto a plane with normal equal to the rotation axis
            Vector3 initialOffset = _previousGrabB.position - _previousGrabA.position;
            Vector3 initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);

            Vector3 targetOffset = grabB.position - grabA.position;
            Vector3 targetVector = Vector3.ProjectOnPlane(targetOffset, rotationAxis);

            Quaternion rotationDelta = new Quaternion();
            rotationDelta.SetFromToRotation(initialVector, targetVector);

            Quaternion initialRotation = targetTransform.rotation;
            Quaternion targetRotation = rotationDelta * targetTransform.rotation;

            // Scale logic
            float activeDistance = targetVector.magnitude;
            if(Mathf.Abs(activeDistance) < 0.0001f) activeDistance = 0.0001f;

            float scalePercentage = activeDistance / _initialDistance;

            float previousScale = _activeScale;
            _activeScale = _initialScale * scalePercentage;

            // Scale constraints
            if(_constraints.MinScale.Constrain)
            {
                _activeScale = Mathf.Max(_constraints.MinScale.Value, _activeScale);
            }
            if(_constraints.MaxScale.Constrain)
            {
                _activeScale = Mathf.Min(_constraints.MaxScale.Value, _activeScale);
            }

            // Apply the positional delta initialCenter -> targetCenter and the
            // rotational delta to the target transform
            Vector3 positionDelta = _capturePosition - initialCenter;
            Vector3 deltaProjectedOnPlaneNormal = Vector3.Dot((positionDelta - initialCenter), rotationAxis) * rotationAxis;
            positionDelta -= deltaProjectedOnPlaneNormal;

            Vector3 planarDelta = Quaternion.Inverse(initialRotation) * positionDelta;
            Vector3 normalDelta = Quaternion.Inverse(initialRotation) * deltaProjectedOnPlaneNormal;
            Vector3 totalDelta = planarDelta + normalDelta;

            Vector3 centerDelta = targetCenter - _capturePosition;
            Vector3 scaleCenterDelta = centerDelta * _activeScale / previousScale;
            Vector3 targetDelta = scaleCenterDelta - centerDelta;

            Quaternion rotationInTargetSpace = Quaternion.Inverse(initialRotation) * targetTransform.rotation;

            _capturePosition = targetRotation * totalDelta + targetCenter - targetDelta;
            targetTransform.rotation = targetRotation * rotationInTargetSpace;
            targetTransform.localScale = _activeScale * Vector3.one;

            Vector3 targetPosition = _capturePosition;
            // Y axis constraints
            if(_constraints.MinY.Constrain)
            {
                targetPosition.y = Mathf.Max(_constraints.MinY.Value, targetPosition.y);
            }
            if(_constraints.MaxY.Constrain)
            {
                targetPosition.y = Mathf.Min(_constraints.MaxY.Value, targetPosition.y);
            }
            targetTransform.position = targetPosition;

            _previousGrabA = grabA;
            _previousGrabB = grabB;
        }

        public void EndTransform() { }

        #region Inject

        public void InjectOptionalPlaneTransform(Transform planeTransform)
        {
            _planeTransform = planeTransform;
        }

        public void InjectOptionalConstraints(TwoGrabPlaneConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
