/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Facebook.WitAi.Interfaces
{
    public interface IDynamicEntitiesProvider
    {
        /// <summary>
        /// Used to get dynamic entities
        /// </summary>
        string ToJSON();
    }
}
