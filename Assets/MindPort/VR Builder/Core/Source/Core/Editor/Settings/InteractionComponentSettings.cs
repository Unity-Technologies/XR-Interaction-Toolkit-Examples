using UnityEngine;
using VRBuilder.Core.Runtime.Utils;

namespace VRBuilder.Editor.Settings
{
    /// <summary>
    /// Settings related to VR Builder's interaction component.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionComponentSettings", menuName = "VR Builder/InteractionComponentSettings", order = 1)]
    public class InteractionComponentSettings : SettingsObject<InteractionComponentSettings>
    {
        [SerializeField]
        [Tooltip("Enables VR Builder's built-in XR Interaction Component. You may want to disable this if you are using a custom interaction component, like a partner integration.")]
        public bool EnableXRInteractionComponent = true;
    }
}
