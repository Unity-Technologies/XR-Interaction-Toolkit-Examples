/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi;
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Aggregates and persists Conduit statistics.
    /// </summary>
    internal class ConduitStatistics
    {
        private const string ConduitSuccessfulGenerationsKey = "ConduitSuccessfulGenerations";
        private const string ConduitSignatureFrequencyKey = "ConduitSignatureFrequency";
        private const string ConduitIncompatibleSignatureFrequencyKey = "ConduitIncompatibleSignatureFrequency";
        private readonly IPersistenceLayer _persistenceLayer;

        public ConduitStatistics(IPersistenceLayer persistenceLayer)
        {
            _persistenceLayer = persistenceLayer;
            Load();
        }

        /// <summary>
        /// A randomly generated ID representing at telemetry report.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Number of successful generations since last reset.
        /// </summary>
        public int SuccessfulGenerations { set; get; }

        /// <summary>
        /// Holds the frequency of method signatures.
        /// Key is signatures in the form: [ReturnTypeId]-[TypeId]:[FrequencyOfType],[TypeId]:[FrequencyOfType].
        /// Value is the number of times this signature was encountered in the last extraction process.
        /// </summary>
        public Dictionary<string, int> SignatureFrequency { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Similar to <see cref="SignatureFrequency"/> but for incompatible methods.
        /// </summary>
        public Dictionary<string, int> IncompatibleSignatureFrequency { get; private set; } = new Dictionary<string, int>();

        /// <summary>
        /// Adds the supplied frequencies to the current collection.
        /// </summary>
        /// <param name="sourceFrequencies">The frequencies to add.</param>
        public void AddFrequencies(Dictionary<string, int> sourceFrequencies)
        {
            AddFrequencies(this.SignatureFrequency, sourceFrequencies);
        }

        /// <summary>
        /// Adds the supplied incompatible method frequencies to the current collection.
        /// </summary>
        /// <param name="sourceFrequencies">The frequencies to add.</param>
        public void AddIncompatibleFrequencies(Dictionary<string, int> sourceFrequencies)
        {
            AddFrequencies(this.IncompatibleSignatureFrequency, sourceFrequencies);
        }

        /// <summary>
        /// Merges two frequency dictionaries.
        /// </summary>
        /// <param name="targetFrequencies">The frequencies we are adding to.</param>
        /// <param name="sourceFrequencies">The frequencies to add.</param>
        private void AddFrequencies(Dictionary<string, int> targetFrequencies, Dictionary<string, int> sourceFrequencies)
        {
            foreach (var entry in sourceFrequencies)
            {
                if (!targetFrequencies.ContainsKey(entry.Key))
                {
                    targetFrequencies[entry.Key] = entry.Value;
                }
                else
                {
                    targetFrequencies[entry.Key] += entry.Value;
                }
            }
        }

        /// <summary>
        /// Persists the statistics to local storage.
        /// </summary>
        public void Persist()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this.SignatureFrequency);
                _persistenceLayer.SetString(ConduitSignatureFrequencyKey, json);

                json = JsonConvert.SerializeObject(this.IncompatibleSignatureFrequency);
                _persistenceLayer.SetString(ConduitIncompatibleSignatureFrequencyKey, json);

                _persistenceLayer.SetInt(ConduitSuccessfulGenerationsKey, SuccessfulGenerations);
            }
            catch (Exception e)
            {
                Debug.Log($"Failed to persist Conduit statistics. {e}");
            }
        }

        /// <summary>
        /// Loads the statistics from local storage.
        /// </summary>
        public void Load()
        {
            try
            {
                SuccessfulGenerations = _persistenceLayer.HasKey(ConduitSuccessfulGenerationsKey)
                    ? _persistenceLayer.GetInt(ConduitSuccessfulGenerationsKey)
                    : 0;

                if (_persistenceLayer.HasKey(ConduitSignatureFrequencyKey))
                {
                    var json = _persistenceLayer.GetString(ConduitSignatureFrequencyKey);
                    SignatureFrequency = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                }
                else
                {
                    SignatureFrequency = new Dictionary<string, int>();
                }

                if (_persistenceLayer.HasKey(ConduitIncompatibleSignatureFrequencyKey))
                {
                    var json = _persistenceLayer.GetString(ConduitIncompatibleSignatureFrequencyKey);
                    IncompatibleSignatureFrequency = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
                }
                else
                {
                    IncompatibleSignatureFrequency = new Dictionary<string, int>();
                }
            }
            catch (Exception e)
            {
                VLog.E($"Failed to load Conduit statistics. {e}");
            }
        }
    }
}
