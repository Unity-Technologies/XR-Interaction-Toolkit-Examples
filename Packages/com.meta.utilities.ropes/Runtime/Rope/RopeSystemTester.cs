// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Text;
using TMPro;
using UnityEngine;

namespace Meta.Utilities.Ropes
{
    public class RopeSystemTester : MonoBehaviour
    {
        [SerializeField] private RopeSystem m_ropeSystem;

        [SerializeField] private TextMeshProUGUI m_text;

        private StringBuilder m_textBuffer = new();

        private bool m_updateText = true;
        private bool m_tied = false;
        private float m_totalAmountSpooled = 0;
        private float m_totalSlackPercentage = 0;
        private int m_totalBendAnchors = 0;
        private float m_totalBendRevolutions = 0;

        private void Update()
        {
            if (m_tied != m_ropeSystem.Tied)
            {
                m_tied = m_ropeSystem.Tied;
                m_updateText = true;
            }

            if (Mathf.Abs(m_totalAmountSpooled - m_ropeSystem.TotalAmountSpooled) > 0.005)
            {
                m_totalAmountSpooled = m_ropeSystem.TotalAmountSpooled;
                m_updateText = true;
            }

            if (Mathf.Abs(m_totalSlackPercentage - m_ropeSystem.TotalSlackPercentage) > 0.005)
            {
                m_totalSlackPercentage = m_ropeSystem.TotalSlackPercentage;
                m_updateText = true;
            }

            if (m_totalBendAnchors != m_ropeSystem.TotalBendAnchors)
            {
                m_totalBendAnchors = m_ropeSystem.TotalBendAnchors;
                m_updateText = true;
            }

            if (Mathf.Abs(m_totalBendRevolutions - m_ropeSystem.TotalBendRevolutions) > 0.005)
            {
                m_totalBendRevolutions = m_ropeSystem.TotalBendRevolutions;
                m_updateText = true;
            }

            if (m_updateText)
            {
                var c = m_tied ? "green" : "red";

                _ = m_textBuffer.Clear()
                    .AppendFormat("Tied: <color=\"{0}\">{1}</color>\n", c, m_tied)
                    .AppendFormat("Spooled: {0:F2}%\n", m_totalAmountSpooled * 100)
                    .AppendFormat("Slack: {0:F2}%\n", m_totalSlackPercentage * 100)
                    .AppendFormat("Bends: {0}\n", m_totalBendAnchors)
                    .AppendFormat("Revolutions: {0:F1}\n", m_totalBendRevolutions);
                m_text.SetText(m_textBuffer);
                m_updateText = false;
            }
        }
    }
}
