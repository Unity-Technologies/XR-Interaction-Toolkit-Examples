// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class ResetTransform : MonoBehaviour
    {
        private Vector3 m_initialPosition;
        private Quaternion m_initialRotation;
        private Vector3 m_initialScale;

        [SerializeField, AutoSet]
        private Rigidbody m_rigidbody;

        private void Awake()
        {
            m_initialPosition = transform.position;
            m_initialRotation = transform.rotation;
            m_initialScale = transform.localScale;
        }

        public void Reset()
        {
            transform.position = m_initialPosition;
            transform.rotation = m_initialRotation;
            transform.localScale = m_initialScale;

            if (m_rigidbody)
            {
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
                m_rigidbody.isKinematic = false;
            }
        }
    }
}

