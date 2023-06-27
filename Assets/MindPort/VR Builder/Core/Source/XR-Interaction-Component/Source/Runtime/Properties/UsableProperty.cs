using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBuilder.Core.Properties;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core.Settings;
using UnityEngine.Events;

namespace VRBuilder.XRInteraction.Properties
{
    /// <summary>
    /// XR implementation of <see cref="IUsableProperty"/>.
    /// </summary>
    [RequireComponent(typeof(GrabbableProperty))]
    public class UsableProperty : LockableProperty, IUsableProperty
    {
        public event EventHandler<EventArgs> UsageStarted;
        public event EventHandler<EventArgs> UsageStopped;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<UsablePropertyEventArgs> useStarted = new UnityEvent<UsablePropertyEventArgs>();

        [SerializeField]
        private UnityEvent<UsablePropertyEventArgs> useEnded = new UnityEvent<UsablePropertyEventArgs>();

        /// <summary>
        /// Returns true if the GameObject is being used.
        /// </summary>
        public virtual bool IsBeingUsed {get; protected set;}

        /// <summary>
        /// Reference to attached <see cref="InteractableObject"/>.
        /// </summary>
        protected InteractableObject Interactable
        {
            get
            {
                if (interactable == false)
                {
                    interactable = GetComponent<InteractableObject>();
                }

                return interactable;
            }
        }

        /// <inheritdoc/>
        public UnityEvent<UsablePropertyEventArgs> UseStarted => useStarted;

        /// <inheritdoc/>
        public UnityEvent<UsablePropertyEventArgs> UseEnded => useEnded;

        private InteractableObject interactable;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            Interactable.activated.AddListener(HandleXRUsageStarted);
            Interactable.deactivated.AddListener(HandleXRUsageStopped);
            
            InternalSetLocked(IsLocked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Interactable.activated.RemoveListener(HandleXRUsageStarted);
            Interactable.deactivated.RemoveListener(HandleXRUsageStopped);
        }

        protected override void Reset()
        {
            base.Reset();
            Interactable.IsUsable = true;
            gameObject.GetComponent<Rigidbody>().isKinematic = InteractionSettings.Instance.MakeGrabbablesKinematic;
        }

        private void HandleXRUsageStarted(ActivateEventArgs arguments)
        {
            IsBeingUsed = true;
            EmitUsageStarted();
        }

        private void HandleXRUsageStopped(DeactivateEventArgs arguments)
        {
            IsBeingUsed = false;
            EmitUsageStopped();
        }

        protected void EmitUsageStarted()
        {
            UsageStarted?.Invoke(this, EventArgs.Empty);
            UseStarted?.Invoke(new UsablePropertyEventArgs());
        }

        protected void EmitUsageStopped()
        {
            UsageStopped?.Invoke(this, EventArgs.Empty);
            UseEnded?.Invoke(new UsablePropertyEventArgs());
        }

        protected override void InternalSetLocked(bool lockState)
        {
            Interactable.IsUsable = lockState == false;
            
            if (IsBeingUsed)
            {
                if (lockState)
                {
                    Interactable.ForceStopInteracting();
                }
            }
        }

        /// <summary>
        /// Instantaneously simulate that the object was used.
        /// </summary>
        public void FastForwardUse()
        {
            if (IsBeingUsed)
            {
                Interactable.ForceStopInteracting();
            }
            else
            {
                EmitUsageStarted();
                EmitUsageStopped();
            }
        }

        /// <inheritdoc/>
        public void ForceSetUsed(bool isUsed)
        {
            if (IsBeingUsed == isUsed)
            {
                return;
            }

            IsBeingUsed = isUsed;
            if (IsBeingUsed)
            {
                EmitUsageStarted();
            }
            else
            {
                EmitUsageStopped();
            }
        }
    }
}