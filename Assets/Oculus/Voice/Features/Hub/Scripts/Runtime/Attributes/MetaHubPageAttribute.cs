/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.Voice.Hub.Interfaces;

namespace Meta.Voice.Hub.Attributes
{
    public class MetaHubPageAttribute : Attribute, IPageInfo
    {
        public string Name { get; private set; }
        public string Context { get; private set; }
        public int Priority { get; private set; }
        public string Prefix { get; private set; }

        
        public MetaHubPageAttribute(string name = null, string context = "", string prefix = "", int priority = 0)
        {
            Name = name;
            Context = context;
            Priority = priority;
            Prefix = prefix;
        }
    }

    public class MetaHubPageScriptableObjectAttribute : MetaHubPageAttribute
    {
        public MetaHubPageScriptableObjectAttribute(string context = "") : base(context: context)
        {
            
        }
    }
}
