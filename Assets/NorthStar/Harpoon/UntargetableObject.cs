// Copyright (c) Meta Platforms, Inc. and affiliates.
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Prevents an object from being hit by the harpoon
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class UntargetableObject : MonoBehaviour
    {
        public float Radius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
