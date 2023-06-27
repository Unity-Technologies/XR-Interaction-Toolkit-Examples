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
    public class WitTraitInfo
    {
        /// <summary>
        /// Trait display name
        /// </summary>
        [SerializeField] public string name;
        /// <summary>
        /// Trait unique identifier
        /// </summary>
        [SerializeField] public string id;
        /// <summary>
        /// Possible trait values
        /// </summary>
        [SerializeField] public WitTraitValueInfo[] values;
    }
}
