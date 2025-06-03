// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Triggers a unity event when an interaction reaches a defined range
    /// </summary>
    public class InteractableTrigger : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] private float m_triggerThreshold = 1, m_releaseThreshold = 0;
        public UnityEvent OnTrigger, OnRelease;
        [SerializeField, AutoSet] private BaseJointInteractable<float> m_jointInteractable;
        [SerializeField] private bool m_useAbsValue = false;
        private bool m_triggered = false;

        private void OnValidate()
        {
            if (m_triggerThreshold < m_releaseThreshold)
            {
                m_triggerThreshold = m_releaseThreshold;
            }
        }
        private void Update()
        {
            var value = m_jointInteractable.Value;
            if (m_useAbsValue)
                value = Mathf.Abs(value);
            if (!m_triggered)
            {
                if (value >= m_triggerThreshold)
                {
                    OnTrigger.Invoke();
                    m_triggered = true;
                }
            }
            else
            {
                if (value <= m_releaseThreshold)
                {
                    OnRelease.Invoke();
                    m_triggered = false;
                }
            }
        }
    }
}
