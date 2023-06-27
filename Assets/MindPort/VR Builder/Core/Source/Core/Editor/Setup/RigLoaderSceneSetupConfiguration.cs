using System.Collections.Generic;
using VRBuilder.Core.Configuration;
using VRBuilder.UX;

namespace VRBuilder.Editor.Setup
{
    /// <summary>
    /// Basic configuration with rig loader.
    /// </summary>
    public class RigLoaderSceneSetupConfiguration : ISceneSetupConfiguration
    {
        /// <inheritdoc/>
        public int Priority => 128;

        /// <inheritdoc/>
        public string Name => "Single user - Rig loader";

        /// <inheritdoc/>
        public string DefaultProcessController => typeof(StandardProcessController).AssemblyQualifiedName;

        /// <inheritdoc/>
        public string RuntimeConfigurationName => typeof(DefaultRuntimeConfiguration).AssemblyQualifiedName;

        /// <inheritdoc/>
        public string Description => "Similar to the default configuration, except there is no rig in the editor scene. The rig is spawned at runtime by " +
            "the INTERACTION_RIG_LOADER object at the DUMMY_USER position. This can be useful for advanced use cases requiring to switch rig at runtime, " +
            "but it makes it harder to customize the rig.";

        /// <inheritdoc/>
        public IEnumerable<string> AllowedExtensionAssemblies => new string[0];

        /// <inheritdoc/>
        public string DefaultConfettiPrefab => "Confetti/Prefabs/MindPortConfettiMachine";

        /// <inheritdoc/>
        public IEnumerable<string> GetSetupNames()
        {
            return new string[]
            {
                "VRBuilder.Editor.RuntimeConfigurationSetup",
                "VRBuilder.Editor.BasicInteraction.RigSetup.RigLoaderSceneSetup",
                "VRBuilder.Editor.UX.ProcessControllerSceneSetup",
                "VRBuilder.Editor.XRInteraction.XRInteractionSceneSetup"
            };
        }
    }
}
