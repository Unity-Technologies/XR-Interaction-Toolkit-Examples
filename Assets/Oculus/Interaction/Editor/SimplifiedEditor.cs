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
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    public class SimplifiedEditor : UnityEditor.Editor
    {
        protected EditorBase _editorDrawer;
        private const string OptionalSection = "Optionals";

        protected virtual void OnEnable()
        {
            _editorDrawer = new EditorBase(serializedObject);

            _editorDrawer.CreateSections(FindCustomSections(serializedObject), true);

            _editorDrawer.CreateSection(OptionalSection, true);
            _editorDrawer.AddToSection(OptionalSection, FindOptionals(serializedObject));
        }

        protected virtual void OnDisable()
        {
        }

        public override void OnInspectorGUI()
        {
            _editorDrawer.DrawFullInspector();
        }

        private static string[] FindOptionals(SerializedObject serializedObject)
        {
            List<AttributedProperty<OptionalAttribute>> props = new List<AttributedProperty<OptionalAttribute>>();
            UnityEngine.Object obj = serializedObject.targetObject;
            if (obj != null)
            {
                FindAttributedSerializedFields(obj.GetType(), props);
            }

            return props.Where(p => (p.attribute.Flags & OptionalAttribute.Flag.DontHide) == 0)
                .Select(p => p.propertyName)
                .ToArray();
        }

        private static Dictionary<string, string[]> FindCustomSections(SerializedObject serializedObject)
        {
            List<AttributedProperty<SectionAttribute>> props = new List<AttributedProperty<SectionAttribute>>();
            UnityEngine.Object obj = serializedObject.targetObject;
            if (obj != null)
            {
                FindAttributedSerializedFields(obj.GetType(), props);
            }

            Dictionary<string, string[]> sections = new Dictionary<string, string[]>();
            var namedSections = props.GroupBy(p => p.attribute.SectionName);
            foreach (var namedSection in namedSections)
            {
                string[] values = namedSection.Select(p => p.propertyName).ToArray();
                sections.Add(namedSection.Key, values);
            }
            return sections;
        }

        private static void FindAttributedSerializedFields<TAttribute>(Type type,
            List<AttributedProperty<TAttribute>> props)
            where TAttribute : PropertyAttribute
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                TAttribute attribute = field.GetCustomAttribute<TAttribute>();
                if (attribute == null)
                {
                    continue;
                }
                if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    continue;
                }
                props.Add(new AttributedProperty<TAttribute>(field.Name, attribute));
            }
            if (typeof(Component).IsAssignableFrom(type.BaseType))
            {
                FindAttributedSerializedFields<TAttribute>(type.BaseType, props);
            }
        }

        private struct AttributedProperty<TAttribute>
            where TAttribute : PropertyAttribute
        {
            public string propertyName;
            public TAttribute attribute;

            public AttributedProperty(string propertyName, TAttribute attribute)
            {
                this.propertyName = propertyName;
                this.attribute = attribute;
            }
        }
    }
}
