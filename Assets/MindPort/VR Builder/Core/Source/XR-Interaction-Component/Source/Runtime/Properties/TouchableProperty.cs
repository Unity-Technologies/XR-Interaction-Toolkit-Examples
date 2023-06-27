using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRBuilder.Core.Properties;
using VRBuilder.BasicInteraction.Properties;
using UnityEngine.Events;

namespace VRBuilder.XRInteraction.Properties
{ 
    /// <summary>
    /// XR implementation of <see cref="ITouchableProperty"/>.
    /// </summary>
    [RequireComponent(typeof(InteractableObject))]
    public class TouchableProperty : LockableProperty, ITouchableProperty
    {
        [Header("Events")]
        [SerializeField]
        private UnityEvent<TouchablePropertyEventArgs> touchStarted = new UnityEvent<TouchablePropertyEventArgs>();

        [SerializeField]
        private UnityEvent<TouchablePropertyEventArgs> touchEnded = new UnityEvent<TouchablePropertyEventArgs>();

        [Obsolete("Use TouchStarted instead.")]
        public event EventHandler<EventArgs> Touched;

        [Obsolete("Use TouchEnded instead.")]
        public event EventHandler<EventArgs> Untouched;

        /// <summary>
        /// Returns true if the GameObject is touched.
        /// </summary>
        public virtual bool IsBeingTouched { get; protected set; }

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
        public UnityEvent<TouchablePropertyEventArgs> TouchStarted => touchStarted;

        /// <inheritdoc />
        public UnityEvent<TouchablePropertyEventArgs> TouchEnded => touchEnded;


        protected override void OnEnable()
        {
            base.OnEnable();
            
            Interactable.hoverEntered.AddListener(HandleXRTouched);
            Interactable.hoverExited.AddListener(HandleXRUntouched);

            InternalSetLocked(IsLocked);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            Interactable.hoverEntered.RemoveListener(HandleXRTouched);
            Interactable.hoverExited.RemoveListener(HandleXRUntouched);

            IsBeingTouched = false;
        }
        
        protected override void Reset()
        {
            base.Reset();
            Interactable.IsTouchable = true;
            Interactable.IsGrabbable = GetComponent<GrabbableProperty>() != null;
            Interactable.IsUsable = GetComponent<UsableProperty>() != null;
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        private void HandleXRTouched(HoverEnterEventArgs arguments)
        {
            if (arguments.interactorObject.transform.root.GetComponentInChildren<UserSceneObject>() != null)
            {
                IsBeingTouched = true;
                EmitTouched();
            }
        }

        private void HandleXRUntouched(HoverExitEventArgs arguments)
        {
            if (arguments.interactorObject.transform.root.GetComponentInChildren<UserSceneObject>() != null)
            {
                IsBeingTouched = false;
                EmitUntouched();
            }            
        }

        protected void EmitTouched()
        {
            Touched?.Invoke(this, EventArgs.Empty);
            TouchStarted?.Invoke(new TouchablePropertyEventArgs());
        }

        protected void EmitUntouched()
        {
            Untouched?.Invoke(this, EventArgs.Empty);
            TouchEnded?.Invoke(new TouchablePropertyEventArgs());
        }

        protected override void InternalSetLocked(bool lockState)
        {
            Interactable.IsTouchable = lockState == false;
            IsBeingTouched &= lockState == false;
        }

        /// <inheritdoc />
        public void ForceSetTouched(bool isTouched)
        {
            if (IsBeingTouched == isTouched)
            {
                return;
            }

            IsBeingTouched = isTouched;
            if (IsBeingTouched)
            {
                EmitTouched();
            }
            else
            {
                EmitUntouched();
            }
        }

        /// <inheritdoc />
        public void FastForwardTouch()
        {
            if (IsBeingTouched)
            {
                Interactable.ForceStopInteracting();
            }
            else
            {
                EmitTouched();
                EmitUntouched();
            }
        }
    }
}