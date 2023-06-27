/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEngine;

namespace Meta.WitAi.Events
{
    public class EventRegistry
    {
        [SerializeField]
        private List<string> _overriddenCallbacks = new List<string>();
        private HashSet<string> _overriddenCallbacksHash;

        public HashSet<string> OverriddenCallbacks
        {
            get
            {
                if (_overriddenCallbacksHash == null)
                {
                    _overriddenCallbacksHash = new HashSet<string>(_overriddenCallbacks);
                }

                return _overriddenCallbacksHash;
            }
        }

        public void RegisterOverriddenCallback(string callback)
        {
            if (!_overriddenCallbacks.Contains(callback))
            {
                _overriddenCallbacks.Add(callback);
                _overriddenCallbacksHash.Add(callback);
            }
        }

        public void RemoveOverriddenCallback(string callback)
        {
            if (_overriddenCallbacks.Contains(callback))
            {
                _overriddenCallbacks.Remove(callback);
                _overriddenCallbacksHash.Remove(callback);
            }
        }

        public bool IsCallbackOverridden(string callback)
        {
            return OverriddenCallbacks.Contains(callback);
        }
    }
}
