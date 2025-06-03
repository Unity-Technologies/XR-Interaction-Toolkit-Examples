// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace Meta.Utilities.Ropes
{
    /// <summary>
    /// Binds part of a rope to this objects transform position (used for things like the harpoon)
    /// </summary>
    public class RopeTransformBinder : MonoBehaviour
    {
        [SerializeField] public int NodeIndex, BindIndex;
        [SerializeField] private BurstRope m_rope;

        [SerializeField] private bool m_lateUpdate = false;

        public BurstRope Rope { get => m_rope; set => m_rope = value; }

        private void OnEnable()
        {
            Enable();
        }
        private void OnDisable()
        {
            if (m_rope == null)
                return;
            var bind = m_rope.Binds[BindIndex];
            bind.Bound = false;
            m_rope.Binds[BindIndex] = bind;
        }

        public void Enable()
        {
            if (m_rope == null)
                return;
            var bind = m_rope.Binds[BindIndex];
            bind.Bound = true;
            m_rope.Binds[BindIndex] = bind;
        }

        private void Update()
        {
            if (m_lateUpdate) return;
            Run();
        }

        protected virtual Vector3 GetPosition() => transform.position;

        private void Run()
        {
            var bind = m_rope.Binds[BindIndex];
            bind.Target = m_rope.ToRopeSpace(GetPosition());
            bind.Index = NodeIndex;
            m_rope.Binds[BindIndex] = bind;
        }
        private void LateUpdate()
        {
            if (!m_lateUpdate) return;
            Run();
        }
    }
}
