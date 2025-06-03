// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Joint that applies the exact amount of force to a rigidbody to reach its target without overshooting
    /// </summary>
    public class CriticallyDampendSpringJoint : MonoBehaviour
    {
        [SerializeField, AutoSet] private Rigidbody m_body;
        [SerializeField, Range(0f, 1f)] public float PositionSpring, PositionDamping;
        [SerializeField, Range(0f, 1f)] public float RotationSpring, RotationDamping;
        [SerializeField] public float MaxVelocity;
        public float MaxForce = float.PositiveInfinity;
        public float MaxAngularAcceleration = float.PositiveInfinity;
        public Vector3 TargetPoint = Vector3.zero;
        public Quaternion TargetRotation = Quaternion.identity;

        public void AddForce()
        {
            if (m_body.isKinematic)
                return;
            m_body.linearVelocity = Vector3.ClampMagnitude(m_body.linearVelocity, MaxVelocity);
            m_body.AddForce(Vector3.ClampMagnitude(GetForce(), MaxForce), ForceMode.Force);
            m_body.AddTorque(GetAngularForce(), ForceMode.Acceleration);
        }

        private Vector3 GetForce()
        {
            var toTarget = m_body.position - TargetPoint;
            return -GetSpringCoef() * PositionSpring * toTarget - GetDampCoef() * PositionDamping * m_body.linearVelocity;
        }

        private Vector3 GetAngularForce()
        {
            var toTarget = (TargetRotation * Quaternion.Inverse(m_body.rotation)).normalized;
            toTarget.ToAngleAxis(out var angle, out var axis);

            if (float.IsInfinity(axis.x))
            {
                axis = Vector3.zero;
                angle = 0;
            }

            if (angle > 180)
                angle -= 360;

            var invDt = 1.0f / Time.fixedDeltaTime;
            var torque = axis * angle * invDt * RotationSpring - m_body.angularVelocity * Mathf.Rad2Deg * RotationDamping;
            torque = Vector3.ClampMagnitude(torque, MaxAngularAcceleration);

            return torque;
        }


        private float GetDampCoef()
        {
            return m_body.mass / Time.fixedDeltaTime;
        }

        private float GetSpringCoef()
        {
            return m_body.mass / (Time.fixedDeltaTime * Time.fixedDeltaTime);
        }
    }
}
