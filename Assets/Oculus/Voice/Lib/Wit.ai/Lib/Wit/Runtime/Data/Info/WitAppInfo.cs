/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.WitAi.Data.Info
{
    // Training
    public enum WitAppTrainingStatus
    {
        Unknown,
        [JsonProperty("done")]
        Done,
        [JsonProperty("scheduled")]
        Scheduled,
        [JsonProperty("ongoing")]
        Ongoing
    }

    // Wit Application Info
    [Serializable]
    public struct WitAppInfo
    {
        /// <summary>
        /// App display name
        /// </summary>
        [Header("App Info")]
        [SerializeField] public string name;
        /// <summary>
        /// App unique identifier
        /// </summary>
        [SerializeField] public string id;
        /// <summary>
        /// App language supported
        /// </summary>
        [SerializeField] public string lang;
        /// <summary>
        /// True if the application is not publicly accessible
        /// </summary>
        [SerializeField] [JsonProperty("private")] public bool isPrivate;
        /// <summary>
        /// App creation date
        /// </summary>
        [SerializeField] [JsonProperty("created_at")] public string createdAt;

        [Header("Training Info")]
        [JsonProperty("training_status")]
        public WitAppTrainingStatus trainingStatus;
        [JsonProperty("last_training_duration_secs")]
        public int lastTrainDuration;
        [JsonProperty("last_trained_at")]
        public string lastTrainedAt;
        [JsonProperty("will_train_at")]
        public string nextTrainAt;

        [Header("NLU Info")]
        /// <summary>
        /// Intents that can be determined by this Wit AI application
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        public WitIntentInfo[] intents;
        /// <summary>
        /// Entities associated with this Wit AI application
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        public WitEntityInfo[] entities;
        /// <summary>
        /// Traits associated with this Wit AI application
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        public WitTraitInfo[] traits;

        [Header("TTS Info")]
        /// <summary>
        /// TTS Voices available for this app on Wit.ai
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        public WitVoiceInfo[] voices;

        /// <summary>
        /// Composer graph information for this app on Wit.ai
        /// </summary>
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        public WitComposerInfo composer;
    }
}
