/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi;
using Meta.WitAi.Json;

namespace Meta.Conduit.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// Generates manifests from the codebase that capture the essence of what we need to expose to the backend.
    /// The manifest includes all the information necessary to train the backend services as well as dispatching the
    /// incoming requests to the right methods with the right parameters.
    /// </summary>
    internal class ManifestGenerator
    {
        /// <summary>
        /// Provides access to available assemblies.
        /// </summary>
        private readonly IAssemblyWalker _assemblyWalker;

        /// <summary>
        /// Mines assemblies for callback methods and entities.
        /// </summary>
        private readonly IAssemblyMiner _assemblyMiner;

        /// <summary>
        /// The manifest version. This would only change if the schema of the manifest changes.
        /// </summary>
        private const string CurrentVersion = "0.1";

        internal ManifestGenerator(IAssemblyWalker assemblyWalker, IAssemblyMiner assemblyMiner)
        {
            this._assemblyWalker = assemblyWalker;
            this._assemblyMiner = assemblyMiner;
        }

        #region API

        /// <summary>
        /// Generate a manifest for assemblies marked with the <see cref="ConduitAssemblyAttribute"/> attribute.
        /// </summary>
        /// <param name="domain">A friendly name to use for this app.</param>
        /// <param name="id">The App ID.</param>
        /// <returns>A JSON representation of the manifest.</returns>
        public string GenerateManifest(string domain, string id)
        {
            VLog.D($"Generate Manifest: {domain}");
            return GenerateManifest(_assemblyWalker.GetTargetAssemblies(), domain, id);
        }

        /// <summary>
        /// Generate a manifest with empty entities and actions lists.
        /// </summary>
        /// <param name="domain">A friendly name to use for this app.</param>
        /// <param name="id">The App ID.</param>
        /// <returns>A JSON representation of the empty manifest.</returns>
        public string GenerateEmptyManifest(string domain, string id)
        {
            return GenerateManifest(new List<IConduitAssembly>(), domain, id);
        }

        /// <summary>
        /// Extract entities and actions from assemblies marked with the <see cref="ConduitAssemblyAttribute"/> attribute.
        /// </summary>
        /// <returns>Extracted Intents list</returns>
        public List<string> ExtractManifestData()
        {
            VLog.D("Extracting manifest actions and entities.");

            var (entities, actions, errorHandlers) = ExtractAssemblyData(_assemblyWalker.GetTargetAssemblies());
            VLog.D($"Extracted {actions.Count} actions and {entities.Count} entities.");

            List<string> transformedActions = new HashSet<string>(actions.Select(v => v.Name).Where(v => !string.IsNullOrEmpty(v))).ToList();

            return transformedActions;
        }

        #endregion

        /// <summary>
        /// Generate a manifest for the supplied assemblies.
        /// </summary>
        /// <param name="assemblies">List of assemblies to process.</param>
        /// <param name="domain">A friendly name to use for this app.</param>
        /// <param name="id">The App ID.</param>
        /// <returns>A JSON representation of the manifest.</returns>
        private string GenerateManifest(IEnumerable<IConduitAssembly> assemblies, string domain, string id)
        {
            VLog.D($"Generating manifest.");

            var (entities, actions, errorHandlers) = ExtractAssemblyData(assemblies);

            var manifest = new Manifest()
            {
                ID = id,
                Version = CurrentVersion,
                Domain = domain,
                Entities = entities,
                Actions = actions,
                ErrorHandlers = errorHandlers
            };

            return JsonConvert.SerializeObject(manifest);
        }
 
        private (List<ManifestEntity>, List<ManifestAction>, List<ManifestErrorHandler>) ExtractAssemblyData(IEnumerable<IConduitAssembly> assemblies)
        {
            var entities = new List<ManifestEntity>();
            var actions = new List<ManifestAction>();
            var errorHandlers = new List<ManifestErrorHandler>();
            _assemblyMiner.Initialize();
            foreach (var assembly in assemblies)
            {
                actions.AddRange(this._assemblyMiner.ExtractActions(assembly));
                entities.AddRange(this._assemblyMiner.ExtractEntities(assembly));
                try
                {
                    errorHandlers.AddRange(this._assemblyMiner.ExtractErrorHandlers(assembly));
                }
                catch (Exception)
                {
                    VLog.W("Conduit App found no error handlers");
                }
            }

            this.PruneUnreferencedEntities(ref entities, actions);

            return (entities, actions, errorHandlers);
        }

        /// <summary>
        /// Returns a list of all assemblies that should be processed.
        /// This currently selects assemblies that are marked with the <see cref="ConduitAssemblyAttribute"/> attribute.
        /// </summary>
        /// <returns>The list of assemblies.</returns>
        private IEnumerable<Assembly> GetTargetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDefined(typeof(ConduitAssemblyAttribute)));
        }

        /// <summary>
        /// Removes unnecessary entities from the manifest to keep it restricted to what is required.
        /// </summary>
        /// <param name="entities">List of all entities. This list will be changed as a result.</param>
        /// <param name="actions">List of all actions.</param>
        private void PruneUnreferencedEntities(ref List<ManifestEntity> entities, List<ManifestAction> actions)
        {
            var referencedEntities = new HashSet<string>();

            foreach (var action in actions)
            {
                foreach (var parameter in action.Parameters)
                {
                    referencedEntities.Add(parameter.EntityType);
                }
            }

            for (var i = 0; i < entities.Count; ++i)
            {
                if (referencedEntities.Contains(entities[i].ID))
                {
                    continue;
                }

                entities.RemoveAt(i--);
            }
        }
    }
}
