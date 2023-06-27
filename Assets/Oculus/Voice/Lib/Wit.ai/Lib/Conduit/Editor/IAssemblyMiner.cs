/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace Meta.Conduit.Editor
{
    internal interface IAssemblyMiner
    {
        /// <summary>
        /// Initializes the miner for a new extraction and resets statistics.
        /// Must be called before extracting entities or actions for a new extraction operation.
        /// Note: Call this only once when making multiple calls to ExtractEntities and ExtractActions from different
        /// assemblies that are part of the same manifest.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Stores the frequency of method signatures.
        /// Key is signatures in the form: [ReturnTypeId]-[TypeId]:[FrequencyOfType],[TypeId]:[FrequencyOfType].
        /// Value is the number of times this signature was encountered in the last extraction process.
        /// </summary>
        Dictionary<string, int> SignatureFrequency { get; }

        /// <summary>
        /// Stores the frequency of method signatures that were incompatible.
        /// Key is signatures in the form: [#][ReturnTypeId]![TypeId]:[FrequencyOfType],[TypeId]:[FrequencyOfType].
        /// The # at the beginning indicates a static method.
        /// Value is the number of times this signature was encountered in the last extraction process.
        /// </summary>
        Dictionary<string, int> IncompatibleSignatureFrequency { get; }

        /// <summary>
        /// Extracts all entities from the assembly. Entities represent the types used as parameters (such as Enums) of
        /// our methods.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        /// <returns>The list of entities extracted.</returns>
        List<ManifestEntity> ExtractEntities(IConduitAssembly assembly);

        /// <summary>
        /// This method extracts all the marked actions (methods) in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        /// <returns>List of actions extracted.</returns>
        List<ManifestAction> ExtractActions(IConduitAssembly assembly);
        
        /// <summary>
        /// This method extracts all the marked error handlers (methods) in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to process.</param>
        /// <returns>List of actions extracted.</returns>
        List<ManifestErrorHandler> ExtractErrorHandlers(IConduitAssembly assembly);

    }
}
