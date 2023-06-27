/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.Voice.Hub.Utilities
{
    internal static class ReflectionUtils
    {
        private const string NAMESPACE_PREFIX = "Meta";

        internal static bool IsValidNamespace(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith(NAMESPACE_PREFIX);

        internal static List<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            var attributeType = typeof(T);

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => IsValidNamespace(type))
                .Where(type => type.GetCustomAttributes(attributeType, false).Length > 0)
                .ToList();
        }
    }
}
