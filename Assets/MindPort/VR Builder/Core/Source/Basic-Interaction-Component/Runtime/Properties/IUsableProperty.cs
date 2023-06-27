using System;
using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using UnityEngine.Events;

namespace VRBuilder.BasicInteraction.Properties
{
    public interface IUsableProperty : ISceneObjectProperty, ILockable
    {
        [Obsolete("Use UseStarted instead.")]
        event EventHandler<EventArgs> UsageStarted;

        [Obsolete("Use UseEnded instead.")]
        event EventHandler<EventArgs> UsageStopped;

        /// <summary>
        /// Called when the object is used.
        /// </summary>
        UnityEvent<UsablePropertyEventArgs> UseStarted { get; }

        /// <summary>
        /// Called when object use has ended.
        /// </summary>
        UnityEvent<UsablePropertyEventArgs> UseEnded { get; }
        
        /// <summary>
        /// Is object currently used.
        /// </summary>
        bool IsBeingUsed { get; }
        
        /// <summary>
        /// Instantaneously simulate that the object was used.
        /// </summary>
        void FastForwardUse();

        /// <summary>
        /// Force this property to a specified use state.
        /// </summary>        
        void ForceSetUsed(bool isUsed);
        
    }

    public class UsablePropertyEventArgs : EventArgs
    {
    }
}
