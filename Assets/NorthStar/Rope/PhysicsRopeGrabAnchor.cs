// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.Utilities;
using Meta.Utilities.Ropes;
using UnityEngine;

namespace NorthStar
{
    public class PhysicsRopeGrabAnchor : RopeGrabAnchor
    {
        [SerializeField, AutoSet] private PhysicsTransformer m_transform;

        private void OnEnable()
        {
            m_transform.OnInteraction += Grab;
            m_transform.OnEndInteraction += EndGrab;
        }

        private void OnDisable()
        {
            m_transform.OnInteraction -= Grab;
            m_transform.OnEndInteraction -= EndGrab;
        }
    }
}
