/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi.Data.Entities
{
    /// <summary>
    /// A configured dynamic entity meant to be placed on dynamic objects.
    /// when the object is enabled this entity will be registered with active
    /// voice services on activation.
    /// </summary>
    public class RegisteredDynamicEntityKeyword : MonoBehaviour
    {
        [SerializeField] private string entity;
        [SerializeField] private WitEntityKeywordInfo keyword;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(keyword.keyword)) return;
            if (string.IsNullOrEmpty(entity)) return;

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                DynamicEntityKeywordRegistry.Instance.RegisterDynamicEntity(entity, keyword);
            }
            else
            {
                VLog.W($"Cannot register {name}: No dynamic entity registry present in the scene." +
                                 $"Please add one and try again.");
            }
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(keyword.keyword)) return;
            if (string.IsNullOrEmpty(entity)) return;

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                DynamicEntityKeywordRegistry.Instance.UnregisterDynamicEntity(entity, keyword);
            }
        }
    }
}
