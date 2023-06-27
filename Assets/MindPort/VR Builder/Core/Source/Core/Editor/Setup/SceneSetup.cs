// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using VRBuilder.Editor.Setup;
using Object = UnityEngine.Object;

namespace VRBuilder.Editor
{
    /// <summary>
    /// This base class is supposed to be implemented by classes which will be called to setup the scene.
    /// Can be used to e.g. setup process classes or interaction frameworks.
    /// </summary>
    /// <remarks>
    /// See <see cref="ProcessConfigurationSetup"/> as a reference.
    /// </remarks>
    public abstract class SceneSetup
    {
        /// <summary>
        /// Identifier key for specific scene setup types,
        /// e.g. for every interaction framework.
        /// </summary>
        public virtual string Key { get; } = null;

        /// <summary>
        /// Priority lets you tweak in which order different <see cref="SceneSetup"/>s will be performed.
        /// The priority is considered from lowest to highest.
        /// </summary>
        public virtual int Priority { get; } = 0;

        /// <summary>
        /// Setup the scene with necessary objects and/or logic.
        /// </summary>
        public abstract void Setup(ISceneSetupConfiguration configuration);

        /// <summary>
        /// Sets up given <paramref name="prefab"/> in current scene.
        /// </summary>
        /// <remarks>Extensions must be omitted. All asset names and paths in Unity use forward slashes, paths using backslashes will not work.</remarks>
        /// <param name="prefab">Name or path to the target resource to setup.</param>
        /// <exception cref="FileNotFoundException">Exception thrown if no prefab can be found in project with given <paramref name="prefab"/>.</exception>
        protected GameObject SetupPrefab(string prefab)
        {
            if (IsPrefabMissingInScene(Path.GetFileName(prefab)))
            {
                GameObject instance = Object.Instantiate(FindPrefab(prefab));
                instance.name = instance.name.Replace("(Clone)", string.Empty);
                return instance;
            }

            return null;
        }

        /// <summary>
        /// Finds and returns a prefab
        /// </summary>
        /// <remarks>Extensions must be omitted. All asset names and paths in Unity use forward slashes, paths using backslashes will not work.</remarks>
        /// <param name="prefab">Name or path to the target resource to setup.</param>
        /// <exception cref="FileNotFoundException">Exception thrown if no prefab can be found in project with given <paramref name="prefab"/>.</exception>
        protected GameObject FindPrefab(string prefab)
        {
            string filter = $"{prefab} t:Prefab";
            string[] prefabsGUIDs = AssetDatabase.FindAssets(filter, null);

            if (prefabsGUIDs.Any() == false)
            {
                throw new FileNotFoundException($"No prefabs found that match \"{prefab}\".");
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(prefabsGUIDs.First());
            string[] brokenPaths = Regex.Split(assetPath, "Resources/");
            string relativePath = brokenPaths.Last().Replace(".prefab", string.Empty);

            return Resources.Load(relativePath, typeof(GameObject)) as GameObject;
        }

        /// <summary>
        /// Returns true if given <paramref name="prefabName"/> is missing in current scene.
        /// </summary>
        protected bool IsPrefabMissingInScene(string prefabName)
        {
            return GameObject.Find(prefabName) == null;
        }
    }
}
