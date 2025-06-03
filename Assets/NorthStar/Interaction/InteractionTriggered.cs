// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace NorthStar
{
    public class InteractionTriggered : MonoBehaviour
    {
        [SerializeField] private BaseJointInteractable<float> m_interactable;
        [SerializeField, Range(0, 1)] private float m_threshold = 1;

        public bool Triggered { get; private set; } = false;

        private void Update()
        {
            if (m_interactable.Value > m_threshold)
                Triggered = true;
        }

        public void ResetTriggered()
        {
            Triggered = false;
        }
    }
}
