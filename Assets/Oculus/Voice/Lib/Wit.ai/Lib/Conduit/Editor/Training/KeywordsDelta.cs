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
    /// <summary>
    /// Represents the difference between two keywords. One local and one on Wit.Ai.
    /// </summary>
    internal struct KeywordsDelta
    {
        public string Keyword;
        public HashSet<string> WitOnlySynonyms;
        public HashSet<string> LocalOnlySynonyms;
        public HashSet<string> AllSynonyms;

        /// <summary>
        /// Returns true if there are no differences between the synonyms of both keywords.
        /// </summary>
        public bool IsEmpty => WitOnlySynonyms.Count == 0 && LocalOnlySynonyms.Count == 0;
    }
}
