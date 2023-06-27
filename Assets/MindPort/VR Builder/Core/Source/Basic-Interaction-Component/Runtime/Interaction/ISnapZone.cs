using VRBuilder.BasicInteraction.Properties;
using UnityEngine;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Base interface for SnapZone, if you add an custom interaction framework add this interface to your SnapZone implementation. 
    /// </summary>
    public interface ISnapZone
    {
        /// <summary>
        /// Returns if the SnapZone is empty.
        /// </summary>
        bool IsEmpty { get; }
        
        /// <summary>
        /// Returns the snapped object, can be null.
        /// </summary>
        ISnappableProperty SnappedObject { get; }
        
        /// <summary>
        /// Position where the object is snapped.
        /// </summary>
        Transform Anchor { get; }
        
        /// <summary>
        /// Returns if the object can be snapped at all.
        /// </summary>
        bool CanSnap(ISnappableProperty target);
        
        /// <summary>
        /// Forces the object to be snapped, returns true when it was snapped successfully.
        /// </summary>
        bool ForceSnap(ISnappableProperty target);
        
        /// <summary>
        /// Forces release the snapped object, will return false when there is no object.
        /// </summary>
        bool ForceRelease();
    }
}