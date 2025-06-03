// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using UnityEngine;
namespace NorthStar
{
    /// <summary>
    /// Script to automate placing colliders on the hand
    /// </summary>
    public class HandColliders : MonoBehaviour
    {
        [SerializeField] private float m_colliderRadius;
        [SerializeField] private List<Transform> m_fingerRoots = new();
        [SerializeField] private Transform m_wrist;
        [SerializeField] private PhysicsMaterial m_physicMaterial;
        private List<CapsuleCollider> m_fingerColliders = new();

        public enum Direction { x, y, z }
        [SerializeField] private Direction m_direction;

        // Start is called before the first frame update
        private void Awake()
        {
            foreach (var finger in m_fingerRoots)
            {
                BuildFinger(finger);
                BuildFingerBone(m_wrist, finger);
            }
        }

        private void OnEnable()
        {
            foreach (Collider collider in m_fingerColliders)
            {
                collider.enabled = true;
            }
        }
        private void OnDisable()
        {
            foreach (Collider collider in m_fingerColliders)
            {
                collider.enabled = false;
            }
        }

        private void BuildFingerBone(Transform root, Transform next)
        {
            var collider = root.gameObject.AddComponent<CapsuleCollider>();
            collider.direction = (int)m_direction;
            collider.height = next.localPosition.magnitude;
            collider.center = next.localPosition / 2;
            collider.radius = m_colliderRadius;
            collider.material = m_physicMaterial;
            m_fingerColliders.Add(collider);
        }

        private void BuildFinger(Transform root)
        {
            while (root.childCount > 0)
            {
                var child = root.GetChild(0);
                BuildFingerBone(root, child);
                root = child;
            }
        }

    }
}