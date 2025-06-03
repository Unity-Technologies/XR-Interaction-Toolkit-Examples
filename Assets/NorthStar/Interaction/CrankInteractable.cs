// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using Meta.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// An interactable in the form of rotating handle where the user is able to rotate the input on one fixed axis which will output the (?)
    /// </summary>
    public class CrankInteractable : BaseJointInteractable<float>
    {
        [SerializeField]
        private float m_minOutput = 0;
        [SerializeField]
        private float m_maxOutput = 1;

        [Space(5), SerializeField]
        private float m_rotationsTillMax = 5;

        [SerializeField]
        private float m_crankEventDegreesPerSecondThreshold = 15f;

        [SerializeField]
        private bool m_clockwise = true;
        [SerializeField] private bool m_resetValueOnLock = true;

        [SerializeField] private bool m_startAtHalfway = false;

        private Vector3 m_previousDirection = Vector3.zero;
        private float m_totalRotation = 0;
        private float m_ratchetBack = 0;
        private bool m_cranking;

        [Space(10), SerializeField, Tooltip("Enables a snapping of the interactable at set degrees along travel path")]
        private bool m_ratchet;
        [SerializeField, Tooltip("Defines how many degrees between ratchet steps")]
        private float m_step = 15;

        [SerializeField] private bool m_clapRotations = false;

        [SerializeField] private UnityEvent m_onRatchet;
        [SerializeField] private UnityEvent m_onCrankStart;
        [SerializeField] private UnityEvent m_onCrankStop;

        private MathUtils.RollingAverage m_averageDegreesPerSecond = new();

        protected override void Update()
        {
            base.Update();

            if (m_requireGrabbing && !IsGrabbing())
            {
                // lock the joint if we require grabbing and aren't
                m_joint.angularXMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                // limit the joint if we're allowed to interact with it but ratcheting is enabled
                m_joint.angularXMotion = m_ratchet ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
            }

            // trigger the crank stop event if we're cranking and the average degrees per second has dropped below the threshold
            if (m_cranking && m_averageDegreesPerSecond.RollingMeanPerSecond < m_crankEventDegreesPerSecondThreshold)
            {
                m_cranking = false;
                m_onCrankStop?.Invoke();
            }

            if (!m_jointRigidbody.IsSleeping())
            {
                var multiplier = m_clockwise ? 1 : -1;

                // if the handle has turned at all
                var forwardHandleDirection = m_joint.transform.rotation * m_joint.secondaryAxis;
                if (forwardHandleDirection != m_previousDirection)
                {
                    // calculate the turn amount in degrees
                    var currentValue = Vector3.Angle(forwardHandleDirection, m_previousDirection);

                    //Negate the value if the rotation is heading counter clockwise
                    var rightHandleDirection = m_joint.transform.rotation * m_tertiaryAxis;
                    if (Vector3.Dot(m_previousDirection, rightHandleDirection) * multiplier > 0)
                    {
                        currentValue = -currentValue;
                    }

                    // update our total rotation
                    m_totalRotation += currentValue;

                    if (m_clapRotations)
                    {
                        m_totalRotation = Mathf.Clamp(m_totalRotation, m_startAtHalfway ? -m_rotationsTillMax * 360 : 0, m_rotationsTillMax * 360);
                    }

                    // ensure we keep a sample of the degrees turned this frame
                    _ = m_averageDegreesPerSecond.AddSample(Math.Abs(currentValue), Time.deltaTime);

                    // trigger the crank start event if we're turning fast enough
                    if (!m_cranking && (!m_requireGrabbing || IsGrabbing()))
                    {
                        if (m_averageDegreesPerSecond.RollingMeanPerSecond >= m_crankEventDegreesPerSecondThreshold)
                        {
                            m_cranking = true;
                            m_onCrankStart?.Invoke();
                        }
                    }

                    if (m_ratchet)
                    {
                        m_ratchetBack += currentValue;
                        if (m_ratchetBack > m_step)
                        {
                            m_onRatchet?.Invoke();
                            m_ratchetBack %= m_step;
                            // assigning secondaryAxis to itself resets relative rotation so that angular limits become a rolling window
                            m_joint.secondaryAxis = m_joint.secondaryAxis;
                        }

                        // tell the crank to pull back to 0 relative rotation if the player lets go
                        m_joint.targetRotation = Quaternion.AngleAxis(0f, Vector3.right);
                    }

                    var value = m_totalRotation.Map(m_startAtHalfway ? -m_rotationsTillMax * 360 : 0, m_rotationsTillMax * 360, m_minOutput, m_maxOutput);
                    Value = m_clampValues ? Mathf.Clamp(value, m_minOutput, m_maxOutput) : value;
                }
                else
                {
                    _ = m_averageDegreesPerSecond.AddSample(0, Time.deltaTime);
                }

                m_previousDirection = m_joint.transform.rotation * m_joint.secondaryAxis;
            }
            else
            {
                m_averageDegreesPerSecond.Reset();
            }
        }

        public override void Lock()
        {
            base.Lock();

            if (m_resetValueOnLock)
            {
                m_totalRotation = 0;
                m_ratchetBack = 0;
            }

            m_averageDegreesPerSecond.Reset();

            if (m_cranking)
            {
                m_cranking = false;
                m_onCrankStop?.Invoke();
            }
        }
    }
}