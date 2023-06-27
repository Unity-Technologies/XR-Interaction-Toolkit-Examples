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
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Editor
{
    public delegate Boolean FieldWiringStrategy(MonoBehaviour monoBehaviour, FieldInfo fieldInfo, Type type);

    public struct ComponentWiringStrategyConfig
    {
        public string FieldName { get; }
        public FieldWiringStrategy[] Methods { get; }

        public ComponentWiringStrategyConfig(string fieldName, FieldWiringStrategy[] methods)
        {
            FieldName = fieldName;
            Methods = methods;
        }
    }

    public class FieldWiringStrategies
    {
        public static bool WireFieldToAncestors(MonoBehaviour monoBehaviour, FieldInfo field, Type targetType)
        {
            for (var transform = monoBehaviour.transform.parent; transform != null; transform = transform.parent)
            {
                var component = transform.gameObject.GetComponent(targetType);
                if (component)
                {
                    field.SetValue(monoBehaviour, component);
                    EditorUtility.SetDirty(monoBehaviour);
                    return true;
                }
            }

            return false;
        }

        public static bool WireFieldToSceneComponent(MonoBehaviour monoBehaviour, FieldInfo field, Type targetType)
        {
            var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootGameObject in rootObjs)
            {
                var component = rootGameObject.GetComponentInChildren(targetType, true);
                if (component != null)
                {
                    field.SetValue(monoBehaviour, component);
                    EditorUtility.SetDirty(monoBehaviour);
                    return true;
                }
            }

            return false;
        }
    }
}
