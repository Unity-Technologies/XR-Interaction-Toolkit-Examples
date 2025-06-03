// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class CameraFacing : MonoBehaviour
    {
        [SerializeField] private bool m_fixY = false;
        private Transform m_cameraTransform;

        private void Awake()
        {
            m_cameraTransform = Camera.main?.transform;
        }

        private void LateUpdate()
        {
            var dir = transform.position - m_cameraTransform.position;
            if (m_fixY)
            {
                dir.y = 0;
            }
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
