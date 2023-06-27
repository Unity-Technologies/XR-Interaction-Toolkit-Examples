using System;
using UnityEngine.Events;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;

namespace VRBuilder.BasicInteraction.Properties
{
    public interface ISnappableProperty : ISceneObjectProperty, ILockable
    {
        [Obsolete("Use AttachedToSnapZone instead.")]
        event EventHandler<EventArgs> Snapped;

        [Obsolete("Use DetachedFromSnapZone instead.")]
        event EventHandler<EventArgs> Unsnapped;

        /// <summary>
        /// Called when the object is snapped to a snap zone.
        /// </summary>
        UnityEvent<SnappablePropertyEventArgs> AttachedToSnapZone { get; }

        /// <summary>
        /// Called when the object is unsnapped from a snap zone.
        /// </summary>
        UnityEvent<SnappablePropertyEventArgs> DetachedFromSnapZone { get; }

        /// <summary>
        /// Is object currently snapped.
        /// </summary>
        bool IsSnapped { get; }
        
        /// <summary>
        /// Will object be locked when it has been snapped.
        /// </summary>
        bool LockObjectOnSnap { get; }
        
        /// <summary>
        /// Zone to snap into.
        /// </summary>
        ISnapZoneProperty SnappedZone { get; set; }

        /// <summary>
        /// Instantaneously simulate that the object was snapped into given <paramref name="snapZone"/>.
        /// </summary>
        /// <param name="snapZone">Snap zone to snap into.</param>
        void FastForwardSnapInto(ISnapZoneProperty snapZone);
    }

    /// <summary>
    /// Event args for <see cref="ISnappableProperty"/> events.
    /// </summary>
    public class SnappablePropertyEventArgs : EventArgs
    {
        public readonly ISnapZoneProperty SnappedZone;
        public readonly ISnappableProperty SnappedObject;

        public SnappablePropertyEventArgs(ISnappableProperty snappedObject, ISnapZoneProperty snappedZone)
        {
            SnappedObject = snappedObject;
            SnappedZone = snappedZone;
        }
    }
}