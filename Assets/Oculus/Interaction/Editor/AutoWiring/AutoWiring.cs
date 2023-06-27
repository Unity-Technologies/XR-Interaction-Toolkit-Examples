/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class AutoWiring
    {
        static AutoWiring()
        {
            UnityObjectAddedBroadcaster.WhenComponentAdded += (component) =>
            {
                MonoBehaviour monoBehaviour = component as MonoBehaviour;
                if(monoBehaviour == null) return;

                if(!_configs.TryGetValue(component.GetType(), out ComponentWiringStrategyConfig[] configs))
                {
                    return;
                }

                foreach (ComponentWiringStrategyConfig config in configs)
                {
                    AutoWireField(monoBehaviour, config.FieldName, config.Methods);
                }
            };

        }

        private static readonly Dictionary<Type, ComponentWiringStrategyConfig[]> _configs = new
            Dictionary<Type, ComponentWiringStrategyConfig[]>();

        public static void Register(Type type, ComponentWiringStrategyConfig[] fieldConfigs)
        {
            _configs.Add(type, fieldConfigs);
        }

        public static void Unregister(Type type)
        {
            _configs.Remove(type);
        }

        public static bool AutoWireField(MonoBehaviour monoBehaviour,
            string fieldName,
            FieldWiringStrategy[] wiringMethods)
        {
            FieldInfo field = FindField(fieldName, monoBehaviour.GetType());
            if (field == null)
            {
                return false;
            }
            UnityEngine.Object value = field.GetValue(monoBehaviour) as UnityEngine.Object;
            if (value != null)
            {
                return false;
            }

            Undo.RecordObject(monoBehaviour, "Autowiring");

            var interfaceAttribute = field.GetCustomAttribute<InterfaceAttribute>();
            var wirableTypes = interfaceAttribute != null ?
                interfaceAttribute.Types :
                new[] {field.FieldType};

            if (wirableTypes != null)
            {
                foreach (var method in wiringMethods)
                {
                    foreach (Type type in wirableTypes)
                    {
                        if (method.Invoke(monoBehaviour, field, type))
                        {
                            Component component = field.GetValue(monoBehaviour) as Component;
                            Debug.Log("Auto-wiring succeeded: " + monoBehaviour.gameObject.name + "::" +
                                      monoBehaviour.GetType().Name + "." + field.Name +
                                      " was linked to " +
                                      component.gameObject.name + "::" + component.GetType().Name);
                            return true;
                        }
                    }
                }
            }

            if (field.GetCustomAttribute<OptionalAttribute>() == null)
            {
                Debug.LogWarning("Auto-wiring failed: no suitable targets for " +
                                 monoBehaviour.gameObject.name + "::" + monoBehaviour.GetType().Name +
                                 "." + field.Name + " could be found.");
            }

            return false;
        }

        private static FieldInfo FindField(string fieldName, Type type)
        {
            if (type == null)
            {
                return null;
            }

            FieldInfo field = type.GetField(fieldName,
                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return FindField(fieldName, type.BaseType);
            }
            return field;
        }
    }
}
