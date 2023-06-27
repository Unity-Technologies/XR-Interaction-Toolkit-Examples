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
    [CustomPropertyDrawer(typeof(WitComposerInfo))]
    public class WitComposerInfoPropertyDrawer : WitPropertyDrawer
    {
        protected override bool FoldoutEnabled => false;    // Show only the name
    }
}
