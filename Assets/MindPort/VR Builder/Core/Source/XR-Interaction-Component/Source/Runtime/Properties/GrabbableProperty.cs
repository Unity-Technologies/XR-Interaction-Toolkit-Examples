using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBuilder.Core.Properties;
using VRBuilder.BasicInteraction.Properties;
using VRBuilder.Core.Settings;
using UnityEngine.Events;

namespace VRBuilder.XRInteraction.Properties
{
    /// <summary>
    /// XR implementation of <see cref="IGrabbableProperty"/>.
    /// </summary>
    [RequireComponent(typeof(TouchableProperty))]
    public class GrabbableProperty : LockableProperty, IGrabbableProperty
    {
        [Header("Events")]
        [SerializeField]
        private UnityEvent<GrabbablePropertyEventArgs> grabStarted = new UnityEvent<GrabbablePropertyEventArgs>();

        [SerializeField]
        private UnityEvent<GrabbablePropertyEventArgs> grabEnded = new UnityEvent<GrabbablePropertyEventArgs>();

        [Obsolete("Use GrabStarted instead.")]
        public event EventHandler<EventArgs> Grabbed;

        [Obsolete("Use GrabEnded instead.")]
        public event EventHandler<EventArgs> Ungrabbed;

        /// <summary>
        /// Returns true if the Interactable of this property is grabbed.
        /// </summary>
        public virtual bool IsGrabbed { get; protected set; }

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

        private InteractableObject interactable;

        /// <inheritdoc />
        public UnityEvent<GrabbablePropertyEventArgs> GrabStarted => grabStarted;

        /// <inheritdoc />
        public UnityEvent<GrabbablePropertyEventArgs> GrabEnded => grabEnded;

        protected override void OnEnable()
        {
            base.OnEnable();

            Interactable.selectEntered.AddListener(HandleXRGrabbed);
            Interactable.selectExited.AddListener(HandleXRUngrabbed);

            InternalSetLocked(IsLocked);
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
        
            Interactable.selectEntered.RemoveListener(HandleXRGrabbed);
            Interactable.selectExited.RemoveListener(HandleXRUngrabbed);

            IsGrabbed = false;
        }

        protected override void Reset()
        {
            base.Reset();
            Interactable.IsGrabbable = true;

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = InteractionSettings.Instance.MakeGrabbablesKinematic;
            rigidbody.useGravity = !InteractionSettings.Instance.MakeGrabbablesKinematic;         
        }

        private void HandleXRGrabbed(SelectEnterEventArgs arguments)
        {
            if(arguments.interactorObject is XRSocketInteractor)
            {
                return;
            }

            IsGrabbed = true;
            EmitGrabbed();
        }

        private void HandleXRUngrabbed(SelectExitEventArgs arguments)
        {
            if (arguments.interactorObject is XRSocketInteractor)
            {
                return;
            }

            IsGrabbed = false;
            EmitUngrabbed();
        }

        protected void EmitGrabbed()
        {
            Grabbed?.Invoke(this, EventArgs.Empty);
            GrabStarted?.Invoke(new GrabbablePropertyEventArgs());
        }

        protected void EmitUngrabbed()
        {
            Ungrabbed?.Invoke(this, EventArgs.Empty);
            GrabEnded?.Invoke(new GrabbablePropertyEventArgs());
        }

        protected override void InternalSetLocked(bool lockState)
        {
            Interactable.IsGrabbable = lockState == false;
            
            if (IsGrabbed)
            {
                if (lockState)
                {
                    Interactable.ForceStopInteracting();
                }
            }
        }

        /// <summary>
        /// Instantaneously simulate that the object was grabbed.
        /// </summary>
        public void FastForwardGrab()
        {
            if (Interactable.isSelected)
            {
                StartCoroutine(UngrabGrabAndRelease());
            }
            else
            {
                EmitGrabbed();
                EmitUngrabbed();
            }
        }

        /// <summary>
        /// Instantaneously simulate that the object was ungrabbed.
        /// </summary>
        public void FastForwardUngrab()
        {
            if (Interactable.isSelected)
            {
                Interactable.ForceStopInteracting();
            }
            else
            {
                EmitGrabbed();
                EmitUngrabbed();
            }
        }

        /// <summary>
        /// Force this property to a specified grabbed state.
        /// </summary>        
        public void ForceSetGrabbed(bool isGrabbed)
        {
            if (IsGrabbed == isGrabbed)
            {
                return;
            }

            IsGrabbed = isGrabbed;
            if (isGrabbed)
            {
                EmitGrabbed();
            }
            else
            {
                EmitUngrabbed();
            }
        }

        private IEnumerator UngrabGrabAndRelease()
        {
            Interactable.ForceStopInteracting();
                
            yield return new WaitUntil(() => Interactable.isHovered == false && Interactable.isSelected == false);

            if (Interactable.interactorsSelecting.Count > 0)
            {
                DirectInteractor directInteractor = (DirectInteractor)Interactable.interactorsSelecting[0];
                directInteractor.AttemptGrab();

                yield return null;

                Interactable.ForceStopInteracting();
            }
        }
    }
}