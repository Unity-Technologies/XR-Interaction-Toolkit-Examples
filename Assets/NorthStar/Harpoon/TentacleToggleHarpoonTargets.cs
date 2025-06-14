// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Manages all harpoon targets on a tentacle
    /// </summary>
    public class TentacleToggleHarpoonTargets : MonoBehaviour
    {
        [SerializeField] private HarpoonTarget[] m_harpoonTargets;
        public UnityEvent OnHitAny;
        private void Awake()
        {
            foreach (var target in m_harpoonTargets)
            {
                target.OnHit.AddListener(OnHitAny.Invoke);
            }
        }
        [ContextMenu("Enable this tentacles harpoon targets")]
        public void EnableHarpoonTargets()
        {
            foreach (var target in m_harpoonTargets)
            {
                target.gameObject.SetActive(true);
            }
        }

        [ContextMenu("Enable this tentacles harpoon targets")]
        public void DisableHarpoonTargets()
        {
            foreach (var target in m_harpoonTargets)
            {
                target.gameObject.SetActive(false);
            }
        }
    }
}
