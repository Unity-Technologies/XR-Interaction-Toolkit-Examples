// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// An interactable in the form of lever where the user is able to manipulate a handle on a hinge which will output the angle value in a ratio from 0 to 1 (limits.min to limits.max)
    /// </summary>
    public class LeverInteractable : BaseJointInteractable<float>
    {
        [Space(10), SerializeField]
        private float m_minOutput = 0;
        [SerializeField]
        private float m_maxOutput = 1;

        [Space(10), SerializeField, Tooltip("Enables a snaping of the interactable at set degrees along travel path")]
        private bool m_ratchet;
        [SerializeField, Tooltip("Defines how many degrees between ratchet steps")]
        private float m_step = 15;

        protected override void Update()
        {
            if (!m_jointRigidbody.IsSleeping())
            {
                // Compare the handle and base's forward vectors to get the rotated angle
                var forwardHandleDirection = m_joint.transform.rotation * m_joint.secondaryAxis;
                var forwardBaseDirection = m_joint.connectedBody.transform.rotation * m_joint.secondaryAxis;
                // Now calculate the angle between to the two vectors
                var currentValue = Vector3.Angle(forwardHandleDirection, forwardBaseDirection);

                // Get a second set of angles using the third axis for the handle
                var rightHandleDirection = m_joint.transform.rotation * m_tertiaryAxis;
                var rightAngle = Vector3.Angle(rightHandleDirection, forwardBaseDirection);

                // Use the second set of angles to calculate the sign of the first set
                if (rightAngle > 90)
                {
                    currentValue = -currentValue;
                }

                if (m_ratchet)
                {
                    //Set joint's drive/target rotation to apply forces along the interactable's path at every set of degrees, set by m_Step
                    var stepOut = Mathf.Round(currentValue / m_step) * m_step;
                    m_joint.targetRotation = Quaternion.AngleAxis(stepOut, Vector3.right); //Vector3.right is used because configurable joint is set to be in local axis, where the x component is the primary axis
                }

                currentValue = currentValue.Map(m_joint.lowAngularXLimit.limit, m_joint.highAngularXLimit.limit, m_minOutput, m_maxOutput);
                if (currentValue != Value)
                {
                    Value = currentValue;
                }
            }
        }
    }
}