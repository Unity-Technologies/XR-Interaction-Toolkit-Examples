using System.Collections.Generic;
using UnityEngine;
using VRBuilder.Unity;

namespace VRBuilder.Core.Configuration
{
    /// <summary>
    /// Handles configuration specific to this scene.
    /// </summary>
    public class SceneConfiguration : MonoBehaviour, ISceneConfiguration
    {
        [SerializeField]
        [Tooltip("Lists all assemblies whose property extensions will be used in the current scene.")]
        private List<string> extensionAssembliesWhitelist = new List<string>();

        [SerializeField]
        [Tooltip("Default resources prefab to use for the Confetti behavior.")]
        private string defaultConfettiPrefab;

        /// <summary>
        /// Lists all assemblies whose property extensions will be used in the current scene.
        /// </summary>
        public IEnumerable<string> ExtensionAssembliesWhitelist => extensionAssembliesWhitelist;

        /// <summary>
        /// Default resources prefab to use for Confetti behavior.
        /// </summary>
        public string DefaultConfettiPrefab
        {
            get { return defaultConfettiPrefab; }
            set { defaultConfettiPrefab = value; }
        }

        /// <summary>
        /// Adds the specified assembly names to the extension whitelist.
        /// </summary>
        public void AddWhitelistAssemblies(IEnumerable<string> assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                if(extensionAssembliesWhitelist.Contains(assemblyName) == false)
                {
                    extensionAssembliesWhitelist.Add(assemblyName);
                }
            }
        }        
    }
}
