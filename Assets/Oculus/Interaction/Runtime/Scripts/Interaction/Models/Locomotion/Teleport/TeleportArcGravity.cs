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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This is a parabolic shot. Gravity and speed are used to
    /// calculate the points along the arc.
    /// It also contains a series of modifiers in order to adjust
    /// the origin of the arc to adapt better in different situations
    /// such as when pointing at your feet, far away or up high
    /// </summary>
    public class TeleportArcGravity : MonoBehaviour, IPolyline
    {
        /// <summary>
        /// The transform from which the arc will be casted
        /// </summary>
        [SerializeField]
        [Tooltip("The transform from which the arc will be casted")]
        private Transform _origin;
        /// <summary>
        /// A point behind the origin used to stabilize the aiming
        /// direction.
        /// </summary>
        [SerializeField]
        [Tooltip("A point behind the origin used to stabilize the aiming direction.")]
        private Transform _stabilizationPoint;

        /// <summary>
        /// Increases the range of the arc based on the distance from
        /// the origin to the stabilization point.
        /// </summary>
        [SerializeField]
        [Tooltip("Increases the range of the arc based on the distance from the origin to the stabilization point.")]
        private AnimationCurve _rangeCurve = new AnimationCurve(
            new Keyframe(0f, 5f),
            new Keyframe(1f, 20f));
        public AnimationCurve RangeCurve
        {
            get
            {
                return _rangeCurve;
            }
            set
            {
                _rangeCurve = value;
            }
        }

        /// <summary>
        /// Mixes the direction of the origin with the
        /// stabilized direction based on the pitch.
        /// </summary>
        [SerializeField]
        [Tooltip("Mixes the direction of the origin with the stabilized direction based on the pitch.")]
        private AnimationCurve _stabilizationMixCurve = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve StabilizationMixCurve
        {
            get
            {
                return _stabilizationMixCurve;
            }
            set
            {
                _stabilizationMixCurve = value;
            }
        }

        /// <summary>
        /// Adjusts the pitch of the origin based on the entry pitch
        /// </summary>
        [SerializeField]
        [Tooltip("Alters the pitch of the origin based on the entry pitch")]
        private AnimationCurve _pitchCurve = new AnimationCurve(
            new Keyframe(-90f, -90f),
            new Keyframe(+90f, +90f));
        public AnimationCurve PitchCurve
        {
            get
            {
                return _pitchCurve;
            }
            set
            {
                _pitchCurve = value;
            }
        }

        /// <summary>
        /// Multiplier for the gravity force
        /// </summary>
        [SerializeField]
        [Tooltip("Multiplier for the gravity force")]
        private float _gravityModifier = 2.3f;
        public float GravityModifier
        {
            get
            {
                return _gravityModifier;
            }
            set
            {
                _gravityModifier = value;
            }
        }

        [SerializeField, Min(2)]
        private int _arcPointsCount = 30;
        public int PointsCount
        {
            get
            {
                return _arcPointsCount;
            }
            set
            {
                _arcPointsCount = value;
            }
        }

        private static readonly Vector3 GRAVITY = new Vector3(0f, -9.81f, 0f);
        private static readonly float GROUND_MARGIN = 2f;

        private Pose _pose = Pose.identity;
        private float _speed = 0f;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_origin, nameof(_origin));
            this.AssertField(_stabilizationPoint, nameof(_stabilizationPoint));
            this.AssertField(_rangeCurve, nameof(_rangeCurve));
            this.AssertField(_stabilizationMixCurve, nameof(_stabilizationMixCurve));
            this.AssertField(_pitchCurve, nameof(_pitchCurve));
            UpdateArcParameters();
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            UpdateArcParameters();
        }

        public Vector3 PointAtIndex(int index)
        {
            float t = index / (_arcPointsCount - 1f);
            return EvaluateGravityArc(_pose, _speed, t);
        }

        private Vector3 EvaluateGravityArc(Pose origin, float speed, float t)
        {
            Vector3 point = origin.position
                + origin.forward * speed * t
                + 0.5f * t * t * GRAVITY * _gravityModifier;
            if (t >= 1f
                && point.y > origin.position.y - GROUND_MARGIN)
            {
                point.y = origin.position.y - GROUND_MARGIN;
            }
            return point;
        }

        private void UpdateArcParameters()
        {
            _pose = CalculatePose();
            _speed = CalculateSpeed(_pose);
        }

        private Pose CalculatePose()
        {
            Pose pose = _origin.GetPose();
            StabilizeDirection(ref pose);
            RemapPitch(ref pose);
            return pose;
        }

        private float CalculateSpeed(Pose pose)
        {
            Vector3 delta = pose.position - _stabilizationPoint.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            return _rangeCurve.Evaluate(distance);
        }

        private void StabilizeDirection(ref Pose pose)
        {
            Vector3 up = _stabilizationPoint.up;
            Vector3 direction = (pose.position - _stabilizationPoint.position).normalized;
            if (direction.sqrMagnitude == 0f)
            {
                direction = _stabilizationPoint.forward;
            }
            Quaternion stabilizedRotation = Quaternion.LookRotation(direction);

            float mixing = Vector3.Dot(direction, up) * 0.5f + 0.5f;
            mixing = _stabilizationMixCurve.Evaluate(mixing);
            Quaternion mixedRotation = Quaternion.Lerp(pose.rotation, stabilizedRotation, mixing);
            pose.rotation = mixedRotation;
        }

        private void RemapPitch(ref Pose pose)
        {
            Vector3 up = _stabilizationPoint.up;
            Vector3 direction = pose.forward;
            Vector3 flatDir = Vector3.ProjectOnPlane(direction, up).normalized;
            Vector3 right = Vector3.Cross(flatDir, up).normalized;
            float angle = Vector3.SignedAngle(flatDir, direction, right);
            angle = _pitchCurve.Evaluate(angle);
            Quaternion delta = Quaternion.AngleAxis(angle, right);

            Vector3 dir = delta * flatDir;
            if (dir.sqrMagnitude != 0)
            {
                pose.rotation = Quaternion.LookRotation(dir, pose.up);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 prevPoint = PointAtIndex(0);
            Gizmos.color = new Color(0f, 1f, 1f, 1f);
            for (int i = 1; i < PointsCount; i++)
            {
                Vector3 point = PointAtIndex(i);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif

        #region Inject
        public void InjectAllTeleportArcGravity(Transform origin, Transform stabilizationPoint)
        {
            InjectOrigin(origin);
            InjectStabilizationPoint(stabilizationPoint);
        }

        public void InjectOrigin(Transform origin)
        {
            _origin = origin;
        }

        public void InjectStabilizationPoint(Transform stabilizationPoint)
        {
            _stabilizationPoint = stabilizationPoint;
        }
        #endregion
    }
}
