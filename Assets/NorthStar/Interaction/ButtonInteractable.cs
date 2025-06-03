// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace NorthStar
{
    public class ButtonInteractable : BaseJointInteractable<float>
    {
        [SerializeField] private float m_buttonHeight = .05f;
        private void OnValidate()
        {
            m_jointRigidbody.transform.localPosition = Vector3.zero + Vector3.up * m_buttonHeight;
            m_joint.targetPosition = m_jointRigidbody.transform.localPosition;
            m_joint.linearLimit = new SoftJointLimit()
            {
                limit = m_buttonHeight
            };
        }

        protected override void Update()
        {
            var value = 1 - m_jointRigidbody.transform.localPosition.y / m_buttonHeight;
            if (Value != value)
                Value = value;
        }
    }
}
