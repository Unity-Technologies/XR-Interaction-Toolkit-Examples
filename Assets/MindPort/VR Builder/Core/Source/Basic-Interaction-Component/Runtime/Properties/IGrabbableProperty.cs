using System;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using UnityEngine.Events;

namespace VRBuilder.BasicInteraction.Properties
{
    public interface IGrabbableProperty : ISceneObjectProperty, ILockable
    {
        /// <summary>
        /// Called when grabbed.
        /// </summary>
        UnityEvent<GrabbablePropertyEventArgs> GrabStarted { get; }

        /// <summary>
        /// Called when ungrabbed.
        /// </summary>
        UnityEvent<GrabbablePropertyEventArgs> GrabEnded { get; }        
        
        /// <summary>
        /// Is object currently grabbed.
        /// </summary>
        bool IsGrabbed { get; }
        
        /// <summary>
        /// Instantaneously simulate that the object was grabbed.
        /// </summary>
        void FastForwardGrab();

        /// <summary>
        /// Instantaneously simulate that the object was ungrabbed.
        /// </summary>
        void FastForwardUngrab();

        /// <summary>
        /// Force this property to a specified grabbed state.
        /// </summary>   
        void ForceSetGrabbed(bool isGrabbed);
    }

    /// <summary>
    /// Event args for <see cref="IGrabbableProperty"/> events.
    /// </summary>
    public class GrabbablePropertyEventArgs : EventArgs
    {
    }
}
