using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Locomotion provider to directly manipulate the XRRig's position and rotation.
    /// </summary>
    public class RigManipulationProvider : LocomotionProvider
    {
        /// <summary>
        /// Sets a new position and rotation for the XR Rig.
        /// </summary>
        /// <param name="destinationPosition">Target position.</param>
        /// <param name="destinationRotation">Target rotation.</param>
        public void SetRigPositionAndRotation(Vector3 destinationPosition, Quaternion destinationRotation)
        {
            if (CanBeginLocomotion() == false)
            {
                return;
            }
            
            XROrigin xrOrigin = system.xrOrigin;
            
            if (xrOrigin != null)
            {
                Vector3 heightAdjustment = xrOrigin.Origin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
                Vector3 cameraDestination = destinationPosition + heightAdjustment;
                
                xrOrigin.MatchOriginUpCameraForward(destinationRotation * Vector3.up, destinationRotation * Vector3.forward);
                xrOrigin.MoveCameraToWorldLocation(cameraDestination);
            }
        }
    }
}
