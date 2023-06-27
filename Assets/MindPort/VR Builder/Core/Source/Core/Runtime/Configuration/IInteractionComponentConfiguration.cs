namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Should be implemented by every interaction component in order to qualify as such.
    /// </summary>
    public interface IInteractionComponentConfiguration
    {
        /// <summary>
        /// Display name of the interaction component.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// True if the interaction component is meant to work with XR.
        /// </summary>
        bool IsXRInteractionComponent { get; }

        /// <summary>
        /// Name of the prefab to be spawned as user rig.
        /// </summary>
        string DefaultRigPrefab { get; }
    }
}
