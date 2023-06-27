using System;
using UnityEngine.Events;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.BasicInteraction.Properties
{
    /// <summary>
    /// Interface for <see cref="ISceneObjectProperty"/>s that can be used for teleport into.
    /// </summary>
    public interface ITeleportationProperty : ISceneObjectProperty, ILockable
    {
        /// <summary>
        /// Emitted when a teleportation action into this <see cref="ISceneObject"/> was done.
        /// </summary>
        [Obsolete("Use TeleportEnded instead.")]
        event EventHandler<EventArgs> Teleported;

        /// <summary>
        /// Emitted when a teleportation action into this <see cref="ISceneObject"/> was done.
        /// </summary>
        UnityEvent<TeleportationPropertyEventArgs> TeleportEnded { get; }

        /// <summary>
        /// Emitted when the teleportation property is initialized.
        /// </summary>
        UnityEvent OnInitialized { get; }

        /// <summary>
        /// True if the teleportation property is ready to be teleported to.
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// True if a teleportation action into this <see cref="ITeleportationProperty"/> was done.
        /// </summary>
        bool WasUsedToTeleport { get; }
        
        /// <summary>
        /// Sets <see cref="WasUsedToTeleport"/> to true.
        /// </summary>
        /// <remarks>
        /// This method is called every time a <see cref="Conditions.TeleportCondition"/> is activate.
        /// </remarks>
        void Initialize();
        
        /// <summary>
        /// Instantaneously simulate that the object was used.
        /// </summary>
        void FastForwardTeleport();

        /// <summary>
        /// Forces the property to the teleported state.
        /// </summary>
        void ForceSetTeleported();
    }

    /// <summary>
    /// Event args for <see cref="ITeleportationProperty"/> events.
    /// </summary>
    public class TeleportationPropertyEventArgs : EventArgs
    {
    }
}
