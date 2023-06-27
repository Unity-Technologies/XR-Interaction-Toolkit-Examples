/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.Events
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class EventCategoryAttribute : PropertyAttribute
    {
        public readonly string Category;

        public EventCategoryAttribute(string category = "")
        {
            Category = category;
        }
    }
}
