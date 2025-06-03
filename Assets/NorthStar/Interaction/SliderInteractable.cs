// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// An interactable in the form of Sliding handle where the user is able to manipulate on one fixed axis which will output the offset in a ratio from 0 to 1 (-linearLimit to linearLimit)
    /// </summary>
    public class SliderInteractable : BaseJointInteractable<float>
    {
        [SerializeField]
        private float m_minOutput = 0;
        [SerializeField]
        private float m_maxOutput = 1;
        [SerializeField]
        private float m_snapToStart = 0.05f;
        [SerializeField]
        private float m_snapToEnd = 0.95f;

        [Space(10), SerializeField, Tooltip("Enables a snapping of the interactable at set unit intervals along travel path")]
        private bool m_ratchet;
        [SerializeField, Tooltip("Defines how many units between ratchet steps")]
        private float m_step = 0.15f;
        public float StepOut;

        protected override void Update()
        {
            base.Update();

            if (!m_jointRigidbody.IsSleeping())
            {
                var localPosition = m_joint.connectedAnchor - m_joint.transform.localPosition;
                var axis = Vector3.Scale(localPosition, m_joint.axis);
                var currentValue = Vector3.Dot(axis, m_joint.axis);

                if (m_ratchet)
                {
                    //Set joint's drive/target rotation to apply forces along the interactable's path at every set of degrees, set by m_Step
                    StepOut = Mathf.Round(currentValue / m_step) * m_step;
                    m_joint.targetPosition = StepOut * Vector3.right + m_joint.connectedBody.transform.localPosition.z * Vector3.right; //Vector3.right is used because configurable joint is set to be in local axis, where the x component is the primary axis
                }

                currentValue = currentValue.Map(-m_joint.linearLimit.limit, m_joint.linearLimit.limit, m_minOutput, m_maxOutput);

                if (currentValue <= m_snapToStart)
                    Value = m_minOutput;
                else if (currentValue >= m_snapToEnd)
                    Value = m_maxOutput;
                else if (!Mathf.Approximately(currentValue, Value))
                    Value = currentValue;
            }
        }
    }
}