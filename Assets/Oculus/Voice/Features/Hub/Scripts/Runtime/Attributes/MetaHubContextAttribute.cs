/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Voice.Hub.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MetaHubContextAttribute : Attribute
    {
        public string Context { get; private set; }
        public int Priority { get; private set; }
        public string LogoPath { get; private set; }
        
        public MetaHubContextAttribute(string context, int priority = 1000, string pathToLogo = "")
        {
            Context = context;
            Priority = priority;
            LogoPath = pathToLogo;
        }
    }
}
