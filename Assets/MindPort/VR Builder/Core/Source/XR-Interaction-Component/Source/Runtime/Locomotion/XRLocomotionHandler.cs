using VRBuilder.BasicInteraction;
using UnityEngine;

namespace VRBuilder.XRInteraction
{
    /// <summary>
    /// Locomotion handler for Unity XR.
    /// </summary>
    [RequireComponent(typeof(RigManipulationProvider))]
    public class XRLocomotionHandler : BaseLocomotionHandler
    {
        private RigManipulationProvider rigManipulationProvider;
        
        /// <summary>
        /// Current rotation of the XR Rig.
        /// </summary>
        public override Quaternion CurrentRotation => RigManipulationProvider.system.xrOrigin.transform.rotation;

        /// <summary>
        /// Current position of the XR Rig.
        /// </summary>
        public override Vector3 CurrentPosition => RigManipulationProvider.system.xrOrigin.transform.position;

        /// <summary>
        /// Locomotion provider to directly manipulate the XR Rig.
        /// </summary>
        protected RigManipulationProvider RigManipulationProvider
        {
            get
            {
                if (rigManipulationProvider == null)
                {
                    rigManipulationProvider = GetComponent<RigManipulationProvider>();
                }

                return rigManipulationProvider;
            }
        }
        
        /// <inheritdoc />
        public override void SetPositionAndRotation(Vector3 destinationPosition, Quaternion destinationRotation)
        {
            RigManipulationProvider.SetRigPositionAndRotation(destinationPosition, destinationRotation);
        }
    }
}