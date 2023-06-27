using System.Collections.Generic;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Handles configuration specific to this scene.
    /// </summary>
    public interface ISceneConfiguration
    {
        /// <summary>
        /// Lists all assemblies whose property extensions will be used in the current scene.
        /// </summary>
        IEnumerable<string> ExtensionAssembliesWhitelist { get; }

        /// <summary>
        /// Default resources prefab to use for Confetti behavior.
        /// </summary>
        string DefaultConfettiPrefab { get; }
    }
}
