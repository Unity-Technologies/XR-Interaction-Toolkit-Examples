// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Object to be put on things the harpoon should hit
    /// </summary>
    public class HarpoonTarget : MonoBehaviour
    {
        public bool Reelable = true;
        [SerializeField] private bool m_invalidAfterHit = true;
        [SerializeField] private bool m_validForAimAssist = true;
        //public bool Sticky = true;
        public UnityEvent OnHit, OnCompleteReel;
        public bool HasBeenHit { get; private set; }
        public bool HasBeenReeled { get; private set; }

        [ContextMenu("Hit This")]
        public void Hit()
        {
            if (HasBeenHit) return;
            HasBeenHit = true;
            OnHit.Invoke();
        }
        public void Reeled()
        {
            if (HasBeenReeled) return;
            HasBeenReeled = true;
            OnCompleteReel.Invoke();
        }

        private void AddToTargets(List<HarpoonTarget> targets)
        {
            if (!m_validForAimAssist) return;
            if (HasBeenHit && m_invalidAfterHit) return;
            targets?.Add(this);
        }

        private void OnEnable()
        {
            Harpoon.FindTargets += AddToTargets;
        }

        private void OnDisable()
        {
            Harpoon.FindTargets -= AddToTargets;
        }
    }
}
