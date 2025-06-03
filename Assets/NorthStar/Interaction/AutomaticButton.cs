// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Physical button interaction
    /// </summary>
    public class AutomaticButton : MonoBehaviour
    {
        public UnityEvent OnPress;
        public UnityEvent OnRelease;

        [SerializeField] private Vector3 m_direction = Vector3.down;
        [SerializeField] private float m_pressTime = 0.2f;
        [SerializeField] private float m_releaseTime = 0.1f;
        [SerializeField] private float m_debounceTimer = 0.2f;

        private bool m_pressed;
        private bool m_locked;
        private float m_debounceTimeRemaining;
        private int m_triggerCount;

        private Vector3 m_releasedPosition;
        private Vector3 m_pressedPosition;
        private float m_pressTimeRemaining;
        private float m_releaseTimeRemaining;

        public bool Pressed => m_pressed;
        public bool Locked => m_locked;

        private void Start()
        {
            m_releasedPosition = transform.localPosition;
            m_pressedPosition = m_releasedPosition + m_direction;
        }

        private void OnTriggerEnter(Collider other)
        {
            // keep track of the number of colliders triggering
            m_triggerCount++;

            // bail if there was already a collider in the trigger 
            if (m_triggerCount > 1) return;

            // ensure we can't double trigger the button
            if (m_debounceTimeRemaining > 0) return;

            // if the button has already been pressed, or it's locked, bail
            if (m_pressed || m_locked) return;

            Press();
        }

        private void OnTriggerExit(Collider other)
        {
            // keep track of the number of colliders triggering
            m_triggerCount--;

            // if this was the final collider to leave, set the debounce timer
            if (m_triggerCount == 0) m_debounceTimeRemaining = m_debounceTimer;
        }

        public void Press()
        {
            if (m_pressed) return;
            m_pressed = true;
            m_pressTimeRemaining = m_pressTime;
            OnPress?.Invoke();
        }

        public void Release()
        {
            if (!m_pressed) return;
            m_pressed = false;
            m_releaseTimeRemaining = m_releaseTime;
            OnRelease?.Invoke();
        }

        public void Lock()
        {
            m_locked = true;
        }

        public void Unlock()
        {
            m_locked = false;
        }

        private void Update()
        {
            if (m_debounceTimeRemaining > 0)
            {
                m_debounceTimeRemaining -= Time.deltaTime;
            }

            if (m_pressTimeRemaining > 0)
            {
                m_pressTimeRemaining -= Time.deltaTime;
                var t = 1 - m_pressTimeRemaining / m_pressTime;
                transform.localPosition = Vector3.Lerp(m_releasedPosition, m_pressedPosition, t);
            }
            else if (m_releaseTimeRemaining > 0)
            {
                m_releaseTimeRemaining -= Time.deltaTime;
                var t = 1 - m_releaseTimeRemaining / m_releaseTime;
                transform.localPosition = Vector3.Lerp(m_pressedPosition, m_releasedPosition, t);
            }
        }
    }
}