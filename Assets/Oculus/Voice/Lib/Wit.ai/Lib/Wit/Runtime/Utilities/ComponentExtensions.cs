/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Meta.WitAi
{
    public static class ComponentExtensions
    {
        // Copy data
        [Serializable]
        public struct ComponentCopyData
        {
            public Type ComponentType;
            public FieldInfo[] Fields;
            public PropertyInfo[] Properties;
        }
        // Component data
        private static Dictionary<Type, ComponentCopyData> _data = new Dictionary<Type, ComponentCopyData>();

        // Safely destroys
        public static void Copy<T>(this T toComponent, T fromComponent) where T : Component
        {
            // Ignore null
            if (toComponent == null)
            {
                return;
            }
            ComponentCopyData copyData = GetCopyData(fromComponent);
            foreach (var field in copyData.Fields)
            {
                field.SetValue(toComponent, field.GetValue(fromComponent));
            }
            foreach (var property in copyData.Properties)
            {
                property.SetValue(toComponent, property.GetValue(fromComponent));
            }
        }
        // Preload component type
        public static void PreloadCopyData<T>(this T thisComponent) where T : Component => GetCopyData(thisComponent);
        // Get copy data
        private static ComponentCopyData GetCopyData<T>(this T thisComponent) where T : Component
        {
            Type componentType = typeof(T);
            if (!_data.ContainsKey(componentType))
            {
                // Generate data
                ComponentCopyData copyData = new ComponentCopyData();
                copyData.ComponentType = componentType;
                // Get non obsolete, public, instance fields
                List<FieldInfo> fields = new List<FieldInfo>();
                foreach (var field in componentType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!IsObsolete(field.CustomAttributes))
                    {
                        fields.Add(field);
                    }
                }
                copyData.Fields = fields.ToArray();
                // Get non obsolete, readable & writable, non name properties
                List<PropertyInfo> properties = new List<PropertyInfo>();
                foreach (var property in componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!IsObsolete(property.CustomAttributes) && property.CanWrite && property.CanRead && !string.Equals(property.Name, "name"))
                    {
                        properties.Add(property);
                    }
                }
                copyData.Properties = properties.ToArray();
                // Apply data
                _data[componentType] = copyData;
            }
            return _data[componentType];
        }
        // Check for obsolete attribute
        private static bool IsObsolete(IEnumerable<CustomAttributeData> attributes)
        {
            return HasCustomAttributes<ObsoleteAttribute>(attributes);
        }
        // Check attributes for obsolete attribute (GetCustomAttributes extension took multiple ms)
        private static bool HasCustomAttributes<TAttribute>(IEnumerable<CustomAttributeData> attributes) where TAttribute : Attribute
        {
            if (attributes != null)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeType == typeof(TAttribute))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
