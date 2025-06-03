// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace NorthStar
{
    // Progressively enables gameobjects over several frames
    // to avoid frame spikes
    public class ThrottledActivator : MonoBehaviour
    {
        // How many frames to wait before enabling the first object
        [SerializeField] private int m_initialDelay = 2;
        // How many frames to wait before each subsequent enable
        [SerializeField] private int m_intervalDelay = 2;

        [SerializeField] private GameObject[] m_targetObjects;

        [SerializeField] private bool m_disableInAwake = false;

        private int m_ticks = 0;

        private void Awake()
        {
            if (m_disableInAwake)
            {
                foreach (var obj in m_targetObjects)
                {
                    obj.SetActive(false);
                }
            }
        }

        private void Update()
        {
            var previousCount = GetCountFromTicks(m_ticks);
            ++m_ticks;
            var currentCount = GetCountFromTicks(m_ticks);
            if (previousCount != currentCount)
            {
                for (var i = previousCount; i < currentCount; i++)
                {
                    m_targetObjects[i].SetActive(true);
                }
            }
            // We have finished enabling targets
            if (currentCount == m_targetObjects.Length)
            {
                enabled = false;
            }
        }

        private int GetCountFromTicks(int ticks)
        {
            ticks -= m_initialDelay;
            if (ticks < 0) return 0;
            var index = m_intervalDelay != 0 ? 1 + ticks / m_intervalDelay : int.MaxValue;
            return Mathf.Clamp(index, 0, m_targetObjects.Length);
        }
    }
}
