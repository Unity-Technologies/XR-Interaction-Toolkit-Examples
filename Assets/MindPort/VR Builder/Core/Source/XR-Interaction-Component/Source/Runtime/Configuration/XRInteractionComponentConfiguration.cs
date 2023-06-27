using VRBuilder.Core.Configuration;

namespace VRBuilder.XRInteraction.Configuration
{
    /// <summary>
    /// Configuration for the default XR interaction component.
    /// </summary>
    public class XRInteractionComponentConfiguration : IInteractionComponentConfiguration
    {
        /// <inheritdoc/>
        public string DisplayName => "XR Interaction Component";

        /// <inheritdoc/>
        public bool IsXRInteractionComponent => true;

        /// <inheritdoc/>
        public string DefaultRigPrefab => "XR_Setup_Action_Based_Hands";
    }
}