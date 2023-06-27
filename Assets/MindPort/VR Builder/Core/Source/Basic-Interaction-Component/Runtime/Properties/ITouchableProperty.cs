using VRBuilder.Core.SceneObjects;
using VRBuilder.Core.Properties;
using System;
using UnityEngine.Events;

namespace VRBuilder.BasicInteraction.Properties
{
    public interface ITouchableProperty : ISceneObjectProperty, ILockable
    {
        /// <summary>
        /// Called when touched.
        /// </summary>        
        UnityEvent<TouchablePropertyEventArgs> TouchStarted { get; }

        /// <summary>
        /// Called when stopped touching.
        /// </summary>
        UnityEvent<TouchablePropertyEventArgs> TouchEnded { get; }

        /// <summary>
        /// Is object currently touched.
        /// </summary>
        bool IsBeingTouched { get; }
        
        /// <summary>
        /// Instantaneously simulate that the object was touched.
        /// </summary>
        void FastForwardTouch();

        /// <summary>
        /// Force this property to a specified touched state.
        /// </summary>   
        void ForceSetTouched(bool isTouched);
    }

    /// <summary>
    /// Event args for <see cref="ITouchableProperty"/> events.
    /// </summary>
    public class TouchablePropertyEventArgs : EventArgs
    {
    }
}