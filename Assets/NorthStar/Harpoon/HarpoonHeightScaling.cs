// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Raises or lowers the harpoon based on the player height
    /// </summary>
    public class HarpoonHeightScaling : MonoBehaviour
    {
        private Vector3 m_originalPosition;
        [SerializeField] private AnimationCurve m_heightCurve;
        [SerializeField] private GameObject m_stepObject;
        [SerializeField] private float m_stepObjectSize = .1f;
        private List<GameObject> m_stepObjectList = new();

        private void Awake()
        {
            m_originalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            GlobalSettings.PlayerSettings.OnPlayerCalibrationChange += SetHeight;
            SetHeight();
        }
        private void OnDisable()
        {
            GlobalSettings.PlayerSettings.OnPlayerCalibrationChange -= SetHeight;
        }

        public void SetHeight()
        {
            var offset = m_heightCurve.Evaluate(GlobalSettings.PlayerSettings.Height);
            transform.localPosition = m_originalPosition + Vector3.up * offset;
            foreach (var stepObject in m_stepObjectList)
            {
                Destroy(stepObject);
            }
            m_stepObjectList.Clear();
            if (offset < 0)
                return;
            var steps = Mathf.CeilToInt(offset / m_stepObjectSize);
            for (var i = 1; i < steps + 1; i++)
            {
                var go = Instantiate(m_stepObject, transform);
                go.transform.localPosition = Vector3.zero - Vector3.up * (m_stepObjectSize * i);
                m_stepObjectList.Add(go);
            }

        }
    }
}
