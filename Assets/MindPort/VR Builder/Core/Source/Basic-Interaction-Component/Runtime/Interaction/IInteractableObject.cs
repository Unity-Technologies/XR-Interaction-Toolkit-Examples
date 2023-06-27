using UnityEngine;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Base interface to determine that the given class is an interactable object.
    /// </summary>
    public interface IInteractableObject
    {
        /// <summary>
        /// GameObject this interactable object is attached to.
        /// </summary>
        GameObject GameObject { get; }
        
        /// <summary>
        /// Determines if this interactable object can be touched.
        /// </summary>
        bool IsTouchable { set; }

        /// <summary>
        /// Determines if this interactable object can be grabbed.
        /// </summary>
        bool IsGrabbable { set; }

        /// <summary>
        /// Determines if this interactable object can be used.
        /// </summary>
        bool IsUsable { set; }
    }
}