using UnityEngine;
using VRBuilder.Core.Runtime.Utils;

namespace VRBuilder.Core.Settings
{
    /// <summary>
    /// Settings related to VR Builder's default interaction behaviour.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionSettings", menuName = "VR Builder/InteractionSettings", order = 1)]
    public class InteractionSettings : SettingsObject<InteractionSettings>
    {
        [SerializeField]
        [Tooltip("If enabled, objects with a Grabbable Property will not react to physics and therefore will not fall to the ground when released.")]
        public bool MakeGrabbablesKinematic = false;
    }
}
