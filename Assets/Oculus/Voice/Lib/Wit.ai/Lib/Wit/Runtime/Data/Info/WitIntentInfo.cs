/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitIntentEntityInfo
    {
        /// <summary>
        /// Entity name
        /// </summary>
        [SerializeField] public string name;
        /// <summary>
        /// Entity unique id
        /// </summary>
        [SerializeField] public string id;
    }

    [Serializable]
    public struct WitIntentInfo
    {
        /// <summary>
        /// Intent display name
        /// </summary>
        [SerializeField] public string id;
        /// <summary>
        /// Intent unique identifier
        /// </summary>
        [SerializeField] public string name;
        /// <summary>
        /// Entities used with this intent
        /// </summary>
        [SerializeField] public WitIntentEntityInfo[] entities;
    }
}
