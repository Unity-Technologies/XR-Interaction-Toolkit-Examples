// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static Meta.Utilities.Narrative.EventLink;
using static UnityEditor.EditorGUI;
using Object = UnityEngine.Object;

namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(EventLink))]
    public class EventLinkDrawer : PropertyDrawer
    {
        private enum MenuState { Unknown, Empty, Populated }

        private readonly Dictionary<SerializedProperty, MenuState> m_menuStatesPerProperty = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 112f;

            var lineRect = position;
            lineRect.height = EditorGUIUtility.singleLineHeight;

            var component = DrawTargetObjectField(property, lineRect);

            lineRect.y += lineRect.height + EditorGUIUtility.standardVerticalSpacing;

            DrawCombinedHandlerControl(lineRect, property, component,
                                       property.FindPropertyRelative("m_fieldName").stringValue);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private Component DrawTargetObjectField(SerializedProperty property, Rect lineRect)
        {
            Component component = null;
            Object currTargetObject = null;
            var targetProp = property.FindPropertyRelative("m_targetObject");

            if (targetProp.objectReferenceValue is Component comp)
            {
                currTargetObject = comp;
                component = comp;
            }

            var displayedTarget
                = currTargetObject is Transform tf ? tf.gameObject : currTargetObject;

            var newTargetObject = ObjectField(lineRect, "On Object", displayedTarget,
                                              typeof(Object), true);

            // if the user selected something new
            if (newTargetObject != displayedTarget
                // and it's not the gameobject of the current component
                && !(currTargetObject is Component c && newTargetObject == c.gameObject)
                // and it's not something besides a gameobject or component
                && !(newTargetObject && newTargetObject is not (GameObject or Component)))
            {
                // then we have a new gameobject selected with no field assigned yet
                currTargetObject = newTargetObject;
                component = null;

                property.FindPropertyRelative("m_fieldName").stringValue = null;
            }

            if (!component && currTargetObject is GameObject currTargetGo)
            {
                component = currTargetGo.transform;
            }

            if (targetProp.objectReferenceValue != component)
            {
                targetProp.objectReferenceValue = component;

                if (component) { m_menuStatesPerProperty[property] = MenuState.Unknown; }
            }

            return component;
        }

        private static void SelectHandler(Component component, string name, Kind kind,
                                          SerializedProperty linkProp)
        {
            linkProp.FindPropertyRelative("m_targetObject").objectReferenceValue = component;
            linkProp.FindPropertyRelative("m_fieldName").stringValue = name;
            linkProp.FindPropertyRelative("m_kind").enumValueIndex = (int)kind;

            if (TaskManager.DebugLogs)
            {
                Debug.Log($"SelectHandler; component: {component.GetType().Name}; name: {name}; "
                          + $"kind: {kind}; path: {linkProp.propertyPath}");
            }

            _ = linkProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawCombinedHandlerControl(Rect position, SerializedProperty property,
                                                Component currObject, string currHandler)
        {
            GenericMenu menu = null;

            if ((!m_menuStatesPerProperty.ContainsKey(property)
                 || m_menuStatesPerProperty[property] == MenuState.Unknown) && currObject)
            {
                menu = GenerateHandlerMenu(property, currObject);

                m_menuStatesPerProperty[property]
                    = menu!.GetItemCount() > 0 ? MenuState.Populated : MenuState.Empty;
            }

            BeginDisabledGroup(!m_menuStatesPerProperty.ContainsKey(property)
                               || m_menuStatesPerProperty[property] != MenuState.Populated);

            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            labelRect.xMin += indentLevel * 15f;

            HandlePrefixLabel(position, labelRect, new GUIContent("Event"));

            var label = !currObject
                ? "(No target GameObject selected)"
                : m_menuStatesPerProperty[property] == MenuState.Empty
                    ? "(No handlers on this GameObject)"
                    : string.IsNullOrWhiteSpace(currHandler)
                        ? "Select handler..."
                        : $"{currHandler}";

            var ctrlRect = position;
            ctrlRect.xMin = labelRect.xMax + 2f;

            if (DropdownButton(ctrlRect, new GUIContent(label), FocusType.Keyboard)
                && m_menuStatesPerProperty[property] == MenuState.Populated && currObject)
            {
                menu ??= GenerateHandlerMenu(property, currObject);

                menu.DropDown(ctrlRect);
            }

            EndDisabledGroup();
        }

        private static GenericMenu GenerateHandlerMenu(SerializedProperty property,
                                                       Component currObject)
        {
            var menu = new GenericMenu();
            var fieldProp = property.FindPropertyRelative("m_fieldName");

            foreach (var c in currObject.GetComponents<Component>())
            {
                var type = c.GetType();
                var events = new List<string>();
                var addedEventHeader = false;

                foreach (var e in type.GetRuntimeEvents())
                {
                    if (!typeof(Action).IsAssignableFrom(e.EventHandlerType)) { continue; }

                    if (!addedEventHeader)
                    {
                        menu.AddDisabledItem(new GUIContent($"{type.Name}/Events"));
                        addedEventHeader = true;
                    }

                    var current = c == currObject && e.Name == fieldProp.stringValue;

                    menu.AddItem(new GUIContent($"{type.Name}/{e.Name}"), current,
                                 () => SelectHandler(c, e.Name, Kind.Event, property));

                    events.Add(e.Name);
                }

                var addedDelegateHeader = false;
                var addedUnityEventHeader = false;

                foreach (var f in type.GetAllFields())
                {
                    var current = c == currObject && f.Name == fieldProp.stringValue;

                    if (events.Contains(f.Name)) { continue; }

                    if (typeof(Action).IsAssignableFrom(f.FieldType))
                    {
                        if (!addedDelegateHeader)
                        {
                            if (addedEventHeader) { menu.AddSeparator($"{type.Name}/"); }

                            menu.AddDisabledItem(new GUIContent($"{type.Name}/Delegates"));
                            addedDelegateHeader = true;
                        }

                        menu.AddItem(new GUIContent($"{type.Name}/{f.Name}"), current,
                                     () => SelectHandler(c, f.Name, Kind.Delegate, property));
                    }

                    if (typeof(UnityEvent).IsAssignableFrom(f.FieldType))
                    {
                        if (!addedUnityEventHeader)
                        {
                            if (addedEventHeader || addedDelegateHeader)
                            {
                                menu.AddSeparator($"{type.Name}/");
                            }

                            menu.AddDisabledItem(new GUIContent($"{type.Name}/UnityEvents"));
                            addedUnityEventHeader = true;
                        }

                        menu.AddItem(new GUIContent($"{type.Name}/{f.Name}"), current,
                                     () => SelectHandler(c, f.Name, Kind.UnityEvent, property));
                    }
                }
            }

            return menu;
        }
    }
}