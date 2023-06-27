/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.Interfaces;
using UnityEngine;

namespace Meta.Voice.Hub
{
    public class MetaHubPage : ScriptableObject, IMetaHubPage, IPageInfo
    {
        /// <summary>
        /// The context this page will fall under
        /// </summary>
        [SerializeField] private string _context;
        /// <summary>
        /// A prefix that will show up before the name of the page. This is a good place to insert page hierarchy etc.
        /// </summary>
        [SerializeField] private string _prefix;
        /// <summary>
        /// The sorting priority of the page
        /// </summary>
        [SerializeField] private int _priority;

        public virtual string Name => name;
        public virtual string Context => _context;
        public virtual int Priority => _priority;
        public virtual string Prefix => _context;
        
        public virtual void OnGUI()
        {
            
        }
    }
}
