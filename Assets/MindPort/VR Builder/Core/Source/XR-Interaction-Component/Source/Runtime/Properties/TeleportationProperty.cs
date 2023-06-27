// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.XRInteraction;

namespace VRBuilder.Core.Properties
{
    /// <summary>
    /// XR implementation of <see cref="ITeleportationProperty"/>.
    /// </summary>
    /// <remarks>
    /// This implementation is based on 'TeleportationAnchor'.
    /// </remarks>
    [RequireComponent(typeof(TeleportationAnchorVRBuilder), typeof(BoxCollider))]
    public class TeleportationProperty : LockableProperty, ITeleportationProperty
    {
        /// <inheritdoc />
        public event EventHandler<EventArgs> Teleported;

        public bool WasUsedToTeleport => wasUsedToTeleport;

        /// <inheritdoc />
        public UnityEvent<TeleportationPropertyEventArgs> TeleportEnded => teleportEnded;

        /// <inheritdoc />
        public UnityEvent OnInitialized => initialized;

        /// <inheritdoc />
        public bool IsActive => active;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<TeleportationPropertyEventArgs> teleportEnded = new UnityEvent<TeleportationPropertyEventArgs>();

        [SerializeField]
        private UnityEvent initialized = new UnityEvent();

        private TeleportationAnchor teleportationInteractable;
        private Renderer[] renderers;
        private bool wasUsedToTeleport;
        private bool active;

        protected void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            teleportationInteractable = GetComponent<TeleportationAnchor>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            teleportationInteractable.teleporting.AddListener(EmitTeleported);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            teleportationInteractable.teleporting.RemoveListener(EmitTeleported);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            active = true;
            wasUsedToTeleport = false;
            initialized?.Invoke();
        }

        /// <inheritdoc />
        public void FastForwardTeleport()
        {
            TeleportRequest teleportRequest = new TeleportRequest
            {
                requestTime = Time.time,
                matchOrientation = teleportationInteractable.matchOrientation,
                destinationPosition = teleportationInteractable.teleportAnchorTransform.position,
                destinationRotation = teleportationInteractable.teleportAnchorTransform.rotation
            };

            if (teleportationInteractable.teleportationProvider != null)
            {
                teleportationInteractable.teleportationProvider.QueueTeleportRequest(teleportRequest);
            }
            else
            {
                Debug.LogError($"The 'TeleportationAnchor' from {name} is missing a reference to 'TeleportationProvider'.", gameObject);
            }

            active = false;
        }

        /// <inheritdoc />
        protected override void InternalSetLocked(bool lockState)
        {
            foreach (Collider collider in teleportationInteractable.colliders)
            {
                collider.enabled = !lockState;
            }

            teleportationInteractable.enabled = !lockState;

            if (renderers != null)
            {
                foreach (Renderer anchorRenderer in renderers)
                {
                    anchorRenderer.enabled = !lockState;
                }
            }
        }

        protected virtual void EmitTeleported(TeleportingEventArgs args)
        {
            if(active && wasUsedToTeleport == false)
            {
                Teleported?.Invoke(this, EventArgs.Empty);
                teleportEnded?.Invoke(new TeleportationPropertyEventArgs());
                active= false;
                wasUsedToTeleport = true;
            }            
        }

        public void ForceSetTeleported()
        {
            EmitTeleported(new TeleportingEventArgs());
        }
    }
}