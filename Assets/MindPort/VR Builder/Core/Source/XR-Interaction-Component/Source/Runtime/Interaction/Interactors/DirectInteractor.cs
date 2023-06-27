using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Interactor used for directly interacting with interactables that are touching. This is handled via trigger volumes
    /// that update the current set of valid targets for this interactor. This component must have a collision volume that is 
    /// set to be a trigger to work.
    /// </summary>
    /// <remarks>Adds extra control over applicable interactions.</remarks>
    public partial class DirectInteractor : XRDirectInteractor
    {
        [SerializeField]
        private bool precisionGrab = true;

        /// <summary>
        /// Toggles precision grab on this interactor.
        /// </summary>
        public bool PrecisionGrab
        {
            get { return precisionGrab; }
            set
            {
                attachTransform.localPosition = initialAttachPosition;
                attachTransform.localRotation = initialAttachRotation;
                precisionGrab = value;
            }
        }

        private Vector3 initialAttachPosition;
        private Quaternion initialAttachRotation;
        private bool forceGrab;

        protected override void Awake()
        {
            base.Awake();
            initialAttachPosition = attachTransform.localPosition;
            initialAttachRotation = attachTransform.localRotation;
        }

        /// <summary>
        /// Gets whether the selection state is active for this interactor. This will check if the controller has a valid selection
        /// state or whether toggle selection is currently on and active.
        /// </summary>
        public override bool isSelectActive
        {
            get
            {
                if (forceGrab)
                {
                    forceGrab = false;
                    return true;
                }

                return base.isSelectActive;
            }
        }

        /// <summary>
        /// Attempts to grab an interactable hovering this interactor without needing to press the grab button on the controller.
        /// </summary>
        public virtual void AttemptGrab()
        {
            forceGrab = true;
        }

        /// <summary>
        /// This method is called by the Interaction Manager
        /// right before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="arguments">Event data containing the Interactable that is being selected.</param>
        /// <remarks>
        /// <paramref name="arguments"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected override void OnSelectEntering(SelectEnterEventArgs arguments)
        {
            InteractableObject interactableObject = arguments.interactableObject as InteractableObject;

            if (precisionGrab && interactableObject!=null && interactableObject.attachTransform == null)
            {
                switch (interactableObject.movementType)
                {
                    case XRBaseInteractable.MovementType.VelocityTracking:
                    case XRBaseInteractable.MovementType.Kinematic:
                        attachTransform.SetPositionAndRotation(interactableObject.transform.position, interactableObject.transform.rotation);
                        break;
                    case XRBaseInteractable.MovementType.Instantaneous:
                        Debug.LogWarning("Precision Grab is currently not compatible with interactable objects with Movement Type configured as Instantaneous.\n"
                        + $"Please change the Movement Type in {interactableObject.name}.", interactableObject);
                        break;
                }
            }

            base.OnSelectEntering(arguments);
        }

        [System.Obsolete("OnSelectEntering(XRBaseInteractable) has been deprecated. Please, upgrade the XR Interaction Toolkit from the Package Manager to the latest available version.")]
        protected new void OnSelectEntering(XRBaseInteractable interactable)
        {
            OnSelectEntering(new SelectEnterEventArgs
            {
                interactable = interactable,
                interactor = this
            });
        }

        [System.Obsolete("OnSelectEnter(XRBaseInteractable) has been deprecated. Please, upgrade the XR Interaction Toolkit from the Package Manager to the latest available version.")]
        protected void OnSelectEnter(XRBaseInteractable interactable)
        {
            OnSelectEntering(new SelectEnterEventArgs
            {
                interactable = interactable,
                interactor = this
            });
        }


        /// <summary>
        /// This method is called by the Interaction Manager
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="arguments">Event data containing the Interactable that is no longer selected.</param>
        /// <remarks>
        /// <paramref name="arguments"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected override void OnSelectExiting(SelectExitEventArgs arguments)
        {
            base.OnSelectExiting(arguments);
            
            if (precisionGrab)
            {
                attachTransform.localPosition = initialAttachPosition;
                attachTransform.localRotation = initialAttachRotation;
            }
        }

        [System.Obsolete("OnSelectExiting(XRBaseInteractable) has been deprecated. Please, upgrade the XR Interaction Toolkit from the Package Manager to the latest available version.")]
        protected new void OnSelectExiting(XRBaseInteractable interactable)
        {
            OnSelectExiting(new SelectExitEventArgs
            {
                interactable = interactable,
                interactor = this
            });
        }

        [System.Obsolete("OnSelectExit(XRBaseInteractable) has been deprecated. Please, upgrade the XR Interaction Toolkit from the Package Manager to the latest available version.")]
        protected void OnSelectExit(XRBaseInteractable interactable)
        {
            OnSelectExiting(new SelectExitEventArgs
            {
                interactable = interactable,
                interactor = this
            });
        }
    }
}
