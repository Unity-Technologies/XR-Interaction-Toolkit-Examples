/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit
{
    /// <summary>
    /// Triggers a method to be executed if it error happens
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class HandleEntityResolutionFailureAttribute : Attribute
    {
        /// <summary>
        /// Triggers a method to be executed if an error is thrown
        /// </summary>
        public HandleEntityResolutionFailureAttribute() 
        {
            
        }
    }
}
