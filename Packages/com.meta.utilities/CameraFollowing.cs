// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace Meta.Utilities
{
    public class CameraFollowing : MonoBehaviour
    {
        [SerializeField] private Vector3 m_offset;

        private Camera m_camera;

        private void Start() => m_camera = Camera.main;

        private void LateUpdate()
        {
            var cameraPosition = m_camera.transform;
            transform.position = cameraPosition.position + m_offset;
        }
    }
}
