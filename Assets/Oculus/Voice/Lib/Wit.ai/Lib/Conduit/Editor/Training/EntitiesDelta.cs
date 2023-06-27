/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Meta.WitAi.Data.Info;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Holds the difference between two entity representations. One on Wit.Ai and the other in local code as an enum.
    /// </summary>
    internal struct EntitiesDelta
    {
        /// <summary>
        /// Keywords only on Wit.Ai.
        /// </summary>
        public HashSet<WitKeyword> WitOnly;
        
        /// <summary>
        /// Keywords on on local.
        /// </summary>
        public HashSet<WitKeyword> LocalOnly;

        /// <summary>
        /// The keywords that are on both but with different details. This list will be empty if either there are no
        /// common keywords or the common ones match in terms of synonyms.
        /// </summary>
        
        public List<KeywordsDelta> Changed;

        /// <summary>
        /// True when the two entities are identical.
        /// </summary>
        public bool IsEmpty => LocalOnly.Count == 0 && WitOnly.Count == 0 && Changed.Count == 0;
    }
}
