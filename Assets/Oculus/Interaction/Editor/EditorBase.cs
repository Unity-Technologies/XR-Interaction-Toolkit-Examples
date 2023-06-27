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
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.Editor
{
    /// <summary>
    /// A utility class for building custom editors with less work required.
    /// </summary>
    public class EditorBase
    {
        private SerializedObject _serializedObject;

        private HashSet<string> _hiddenProperties = new HashSet<string>();
        private HashSet<string> _skipProperties = new HashSet<string>();

        private Dictionary<string, Action<SerializedProperty>> _customDrawers =
            new Dictionary<string, Action<SerializedProperty>>();

        private Dictionary<string, Section> _sections =
            new Dictionary<string, Section>();
        private List<string> _orderedSections = new List<string>();

        public class Section
        {
            public string title;
            public bool isFoldout;
            public bool foldout;
            public List<string> properties = new List<string>();

            public bool HasSomethingToDraw()
            {
                return properties != null && properties.Count > 0;
            }
        }

        public EditorBase(SerializedObject serializedObject)
        {
            _serializedObject = serializedObject;
        }

        #region Sections
        private Section GetOrCreateSection(string sectionName)
        {
            if (_sections.TryGetValue(sectionName, out Section existingSection))
            {
                return existingSection;
            }

            Section section = CreateSection(sectionName, false);
            return section;
        }

        public Section CreateSection(string sectionName, bool isFoldout)
        {
            if (_sections.TryGetValue(sectionName, out Section existingSection))
            {
                Debug.LogError($"Section {sectionName} already exists");
                return null;
            }

            Section section = new Section() { title = sectionName, isFoldout = isFoldout };
            _sections.Add(sectionName, section);
            _orderedSections.Add(sectionName);
            return section;
        }

        public void CreateSections(Dictionary<string, string[]> sections, bool isFoldout)
        {
            foreach (var sectionData in sections)
            {
                CreateSection(sectionData.Key, isFoldout);
                AddToSection(sectionData.Key, sectionData.Value);
            }
        }

        public void AddToSection(string sectionName, params string[] properties)
        {
            if (properties.Length == 0
                || !ValidateProperties(properties))
            {
                return;
            }

            Section section = GetOrCreateSection(sectionName);
            foreach (var property in properties)
            {
                section.properties.Add(property);
                _skipProperties.Add(property);
            }
        }

        #endregion

        #region API

        /// <summary>
        /// Call in OnEnable with one or more property names to hide them from the inspector.
        ///
        /// This is preferable to using [HideInInspector] because it still allows the property to
        /// be viewed when using the Inspector debug mode.
        /// </summary>
        public void Hide(params string[] properties)
        {
            Assert.IsTrue(properties.Length > 0, "Should always hide at least one property.");
            if (!ValidateProperties(properties))
            {
                return;
            }

            _hiddenProperties.UnionWith(properties);
        }

        /// <summary>
        /// Call in OnInit to specify a custom drawer for a single property.  Whenever the property is drawn,
        /// it will use the provided property drawer instead of the default one.
        /// </summary>
        public void Draw(string property, Action<SerializedProperty> drawer)
        {
            if (!ValidateProperties(property))
            {
                return;
            }

            _customDrawers.Add(property, drawer);
        }

        /// <summary>
        /// Call in OnInit to specify a custom drawer for a single property.  Include an extra property that gets
        /// lumped in with the primary property.  The extra property is not drawn normally, and is instead grouped in
        /// with the primary property.  Can be used in situations where a collection of properties need to be drawn together.
        /// </summary>
        public void Draw(string property,
            string withExtra0,
            Action<SerializedProperty, SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0))
            {
                return;
            }

            Hide(withExtra0);
            Draw(property, p =>
            {
                drawer(p,
                    _serializedObject.FindProperty(withExtra0));
            });
        }

        public void Draw(string property,
            string withExtra0,
            string withExtra1,
            Action<SerializedProperty, SerializedProperty, SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Draw(property, p =>
            {
                drawer(p,
                    _serializedObject.FindProperty(withExtra0),
                    _serializedObject.FindProperty(withExtra1));
            });
        }

        public void Draw(string property,
            string withExtra0,
            string withExtra1,
            string withExtra2,
            Action<SerializedProperty, SerializedProperty, SerializedProperty, SerializedProperty>
                drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1, withExtra2))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Hide(withExtra2);
            Draw(property, p =>
            {
                drawer(p,
                    _serializedObject.FindProperty(withExtra0),
                    _serializedObject.FindProperty(withExtra1),
                    _serializedObject.FindProperty(withExtra2));
            });
        }

        public void Draw(string property,
            string withExtra0,
            string withExtra1,
            string withExtra2,
            string withExtra3,
            Action<SerializedProperty, SerializedProperty, SerializedProperty, SerializedProperty,
                SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1, withExtra2, withExtra3))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Hide(withExtra2);
            Hide(withExtra3);
            Draw(property, p =>
            {
                drawer(p,
                    _serializedObject.FindProperty(withExtra0),
                    _serializedObject.FindProperty(withExtra1),
                    _serializedObject.FindProperty(withExtra2),
                    _serializedObject.FindProperty(withExtra3));
            });
        }

        #endregion

        #region IMPLEMENTATION

        /// <summary>
        /// Indicates if the property in the serializedObject
        /// has been assigned to a section
        /// </summary>
        /// <param name="property">The name of the property in the serialized object</param>
        /// <returns>True if the property has been added to a section</returns>
        public bool IsInSection(string property)
        {
            return _skipProperties.Contains(property);
        }

        /// <summary>
        /// Indicates if the property in the serializedObject
        /// needs to be hidden.
        /// Hidden properties are typically drawn in a custom way
        /// so they don't need to be drawn with the default methods.
        /// </summary>
        /// <param name="property">The name of the property in the serialized object</param>
        /// <returns>True if the property has been hidden</returns>
        public bool IsHidden(string property)
        {
            return _hiddenProperties.Contains(property);
        }

        /// <summary>
        /// Draws all the visible (non hidden)properties in the serialized
        /// object following this order:
        /// First all properties that has not been added to sections,
        /// in the order they appear in the Component.
        /// Then all the sections (indented and in foldouts) in the order
        /// they were created, with the internal properties ordered in the
        /// order the properties were added to the section.
        ///
        /// If a special property drawer was specified it will use it when
        /// drawing said property.
        ///
        /// If a property was hidden, it will not present it in any section.
        /// </summary>
        public void DrawFullInspector()
        {
            SerializedProperty it = _serializedObject.GetIterator();
            it.NextVisible(enterChildren: true);

            //Draw script header
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(it);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();

            while (it.NextVisible(enterChildren: false))
            {
                //Don't draw skip properties in this pass, we will draw them after everything else
                if (IsInSection(it.name))
                {
                    continue;
                }

                DrawProperty(it);
            }

            foreach (string sectionKey in _orderedSections)
            {
                DrawSection(sectionKey);
            }

            _serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws all the properties in the section in the order
        /// they were added.
        /// </summary>
        /// <param name="sectionName">The name of the section</param>
        public void DrawSection(string sectionName)
        {
            Section section = _sections[sectionName];
            DrawSection(section);
        }

        private void DrawSection(Section section)
        {
            if (!section.HasSomethingToDraw())
            {
                return;
            }

            if (section.isFoldout)
            {
                section.foldout = EditorGUILayout.Foldout(section.foldout, section.title);
                if (!section.foldout)
                {
                    return;
                }
                EditorGUI.indentLevel++;
            }

            foreach (string prop in section.properties)
            {
                DrawProperty(_serializedObject.FindProperty(prop));
            }

            if (section.isFoldout)
            {
                EditorGUI.indentLevel--;
            }
        }

        private void DrawProperty(SerializedProperty property)
        {
            try
            {
                //Don't draw hidden properties
                if (IsHidden(property.name))
                {
                    return;
                }

                //Then draw the property itself, using a custom drawer if needed
                Action<SerializedProperty> customDrawer;
                if (_customDrawers.TryGetValue(property.name, out customDrawer))
                {
                    customDrawer(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, includeChildren: true);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error drawing property {e.Message}");
            }
        }

        private bool ValidateProperties(params string[] properties)
        {
            foreach (var property in properties)
            {
                if (_serializedObject.FindProperty(property) == null)
                {
                    Debug.LogWarning(
                        $"Could not find property {property}, maybe it was deleted or renamed?");
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
