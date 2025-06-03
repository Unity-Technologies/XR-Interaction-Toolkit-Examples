// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using System.Linq;
using Meta.Utilities.Ropes;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Measure the tightness of a rope :O
    /// </summary>
    public class RopeTightnessMesurerer : MonoBehaviour
    {
        public float Value { get; private set; }
        public UnityEvent<float> OnInteract;
        [SerializeField] private BurstRope m_target;
        [SerializeField] private float m_baseAvgLength = .1f;
        [SerializeField] private float m_maxAvgLength = .2f;
        [SerializeField] private int m_resultListSize = 10;
        private List<float> m_results = new();

        private void Update()
        {
            if (m_results.Count > m_resultListSize) m_results.RemoveAt(0);

            var lowestBind = m_target.Binds.Count - 1;
            foreach (var bind in m_target.Binds)
            {
                if (bind.Bound && bind.Index < lowestBind && bind.Index != 0)
                {
                    lowestBind = bind.Index;
                }
            }
            var distance = m_target.AverageLinkDistance(0, lowestBind);
            m_results.Add(distance);

            var tightness = Mathf.Clamp(m_results.Average(), m_baseAvgLength, m_maxAvgLength);

            Value = (tightness - m_baseAvgLength) / (m_maxAvgLength - m_baseAvgLength);
            OnInteract.Invoke(Value);
        }
    }
}
