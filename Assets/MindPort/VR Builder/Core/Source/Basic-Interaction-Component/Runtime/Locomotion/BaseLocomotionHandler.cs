using UnityEngine;

namespace VRBuilder.BasicInteraction
{
    /// <summary>
    /// Handles basic locomotion for e.g. rigs and provides an abstract locomotion layer.
    /// </summary>
    public abstract class BaseLocomotionHandler : MonoBehaviour
    {
        /// <summary>
        /// Current rotation of the rig or camera.
        /// </summary>
        public abstract Quaternion CurrentRotation { get; }
        
        /// <summary>
        /// Current position of the rig or camera.
        /// </summary>
        public abstract Vector3 CurrentPosition { get; }
        
        /// <summary>
        /// Sets a new position and rotation for the rig or camera.
        /// </summary>
        /// <param name="destinationPosition">Target position.</param>
        /// <param name="destinationRotation">Target rotation.</param>
        public abstract void SetPositionAndRotation(Vector3 destinationPosition, Quaternion destinationRotation);
    }
}