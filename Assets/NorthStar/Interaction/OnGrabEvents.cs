// Copyright (c) Meta Platforms, Inc. and affiliates.
using Meta.Utilities;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

namespace NorthStar
{
    /// <summary>
    /// Exposes physics transformer callbacks as unity events
    /// </summary>
    public class OnGrabEvents : MonoBehaviour
    {
        [SerializeField, AutoSet] private PhysicsTransformer m_physicsTransformer;

        public UnityEvent OnGrab = new();
        public UnityEvent OnRelease = new();

        private void OnEnable()
        {
            m_physicsTransformer.OnInteraction += Grab;
            m_physicsTransformer.OnEndInteraction += EndGrab;
        }

        private void OnDisable()
        {
            m_physicsTransformer.OnInteraction -= Grab;
            m_physicsTransformer.OnEndInteraction -= EndGrab;
        }

        private void EndGrab(HandGrabInteractor interactor)
        {
            OnRelease.Invoke();
        }

        private void Grab(HandGrabInteractor interactor)
        {
            OnGrab.Invoke();
        }
    }
}
