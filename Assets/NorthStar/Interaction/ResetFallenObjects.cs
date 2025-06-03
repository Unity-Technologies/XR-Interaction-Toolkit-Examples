// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections;
using UnityEngine;

namespace NorthStar
{
    /// <summary>
    /// Resets objects to their original position after they fall
    /// </summary>
    public class ResetFallenObjects : MonoBehaviour
    {
        [SerializeField] private Transform m_originalPositionMarker;
        [SerializeField] private float m_minimumYDistance, m_floorTimeout;
        private float m_onFloorTimer;

        [SerializeField] private bool m_setKinematicOnReset;

        private void Update()
        {
            var toCurrentPosition = transform.position - m_originalPositionMarker.position;
            if (Vector3.Dot(m_originalPositionMarker.up, toCurrentPosition) < m_minimumYDistance)
            {
                m_onFloorTimer += Time.deltaTime;
                if (m_onFloorTimer > m_floorTimeout)
                {
                    transform.position = m_originalPositionMarker.position;
                    transform.rotation = m_originalPositionMarker.rotation;
                    if (m_setKinematicOnReset && TryGetComponent(out Rigidbody rb))
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic = true;
                        _ = StartCoroutine(DisableKinematic());
                    }
                    m_onFloorTimer = 0;
                }
            }
            else
            {
                m_onFloorTimer = 0;
            }
        }

        private IEnumerator DisableKinematic()
        {
            yield return null;
            yield return null;
            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (m_originalPositionMarker == null)
                return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(m_originalPositionMarker.position, m_originalPositionMarker.position + m_originalPositionMarker.up * m_minimumYDistance);
        }
    }
}
