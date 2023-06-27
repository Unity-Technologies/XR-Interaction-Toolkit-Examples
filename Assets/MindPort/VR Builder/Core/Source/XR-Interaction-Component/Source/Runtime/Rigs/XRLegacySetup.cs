namespace VRBuilder.Components.Runtime.Rigs
{
    /// <summary>
    /// Setup for XR with the old input system.
    /// </summary>
    public class XRLegacySetup : XRSetupBase
    {
        /// <inheritdoc />
        public override string Name { get; } = "XR Legacy Rig";
        
        /// <inheritdoc />
        public override string PrefabName { get; } = "XR_Setup_Device_Based";

        /// <inheritdoc />
        public override bool CanBeUsed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return IsEventManagerInScene() == false && IsPrefabMissing == false;
#else
            return false;
#endif
        }

        /// <inheritdoc />
        public override string GetSetupTooltip()
        {
            if (IsPrefabMissing)
            {
                return $"The prefab {PrefabName} is missing in the Resources folder.";
            }
            
#if ENABLE_LEGACY_INPUT_MANAGER
            return "Can't be used while there is already a XRInteractionManager in the scene.";
#else
            return "Enable the legacy input system to allow using this rig.";
#endif
        }
    }
}
