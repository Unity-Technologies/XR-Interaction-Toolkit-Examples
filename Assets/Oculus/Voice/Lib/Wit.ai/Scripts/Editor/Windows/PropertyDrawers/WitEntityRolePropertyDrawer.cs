/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi.Windows
{
    [CustomPropertyDrawer(typeof(WitEntityRoleInfo))]
    public class WitEntityRolePropertyDrawer : WitSimplePropertyDrawer
    {
        // Key = Name
        protected override string GetKeyFieldName()
        {
            return "name";
        }
        // Value = ID
        protected override string GetValueFieldName()
        {
            return "id";
        }
    }
}
