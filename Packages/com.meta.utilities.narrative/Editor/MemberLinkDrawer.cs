// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Meta.Utilities.Narrative.MemberLink<bool>;
using static UnityEditor.EditorGUI;
using Object = UnityEngine.Object;

namespace Meta.Utilities.Narrative
{
    [CustomPropertyDrawer(typeof(MemberLink<>))]
    public class MemberLinkDrawer : PropertyDrawer
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

            DrawCombinedMemberControl(lineRect, property, component,
                                      property.FindPropertyRelative("m_memberName").stringValue);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private Component DrawTargetObjectField(SerializedProperty property, Rect lineRect)
        {
            Component component = null;
            Object currTargetObject = null;

            var targetProp = property.FindPropertyRelative("m_targetObject");
            var memberProp = property.FindPropertyRelative("m_memberName");

            if (targetProp.objectReferenceValue is Component comp)
            {
                currTargetObject = comp;
                component = comp;
            }

            var displayedTarget
                = currTargetObject is Component cc
                  && string.IsNullOrWhiteSpace(memberProp.stringValue)
                    ? cc.gameObject
                    : currTargetObject;

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
                memberProp.stringValue = null;
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

        private static void SelectMember(Component component, string name, Kind kind,
                                         SerializedProperty linkProp)
        {
            linkProp.FindPropertyRelative("m_targetObject").objectReferenceValue = component;
            linkProp.FindPropertyRelative("m_memberName").stringValue = name;
            linkProp.FindPropertyRelative("m_kind").enumValueIndex = (int)kind;

            if (TaskManager.DebugLogs)
            {
                Debug.Log($"SelectMember; component: {component.GetType().Name}; name: {name}; "
                          + $"kind: {kind}; path: {linkProp.propertyPath}");
            }

            _ = linkProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawCombinedMemberControl(Rect position, SerializedProperty property,
                                               Component currObject, string currMember)
        {
            GenericMenu menu = null;

            if ((!m_menuStatesPerProperty.ContainsKey(property)
                 || m_menuStatesPerProperty[property] == MenuState.Unknown) && currObject)
            {
                menu = GenerateMemberMenu(property, currObject);

                m_menuStatesPerProperty[property]
                    = menu!.GetItemCount() > 0 ? MenuState.Populated : MenuState.Empty;
            }

            BeginDisabledGroup(!m_menuStatesPerProperty.ContainsKey(property)
                               || m_menuStatesPerProperty[property] != MenuState.Populated);

            var labelRect = position;
            labelRect.width = EditorGUIUtility.labelWidth;
            labelRect.xMin += indentLevel * 15f;

            HandlePrefixLabel(position, labelRect, new GUIContent("Member"));

            var label = !currObject
                ? "(No target GameObject selected)"
                : m_menuStatesPerProperty[property] == MenuState.Empty
                    ? "(No members of correct type on this GameObject)"
                    : string.IsNullOrWhiteSpace(currMember)
                        ? "Select member..."
                        : $"{MemberLabel(currMember)}";

            var ctrlRect = position;
            ctrlRect.xMin = labelRect.xMax + 2f;

            if (DropdownButton(ctrlRect, new GUIContent(label), FocusType.Keyboard)
                && m_menuStatesPerProperty[property] == MenuState.Populated && currObject)
            {
                (menu ?? GenerateMemberMenu(property, currObject)).DropDown(ctrlRect);
            }

            EndDisabledGroup();

            string MemberLabel(string memberName)
            {
                foreach (var memberInfo in currObject.GetType().GetMember(memberName))
                {
                    // if this is a method and not a property accessor, add () to the end
                    if (memberInfo is MethodInfo methodInfo
                        && !methodInfo.Name.StartsWith("get_")
                        && !methodInfo.Name.StartsWith("set_"))
                    {
                        memberName = $"{memberName}()";
                    }

                    return $"{memberInfo.DeclaringType?.Name}.{memberName}";
                }

                return memberName;
            }
        }

        private GenericMenu GenerateMemberMenu(SerializedProperty property, Component currObject)
        {
            var linkValueType = fieldInfo.FieldType.GetGenericArguments()[0];
            var menu = new GenericMenu();
            var memberProp = property.FindPropertyRelative("m_memberName");

            foreach (var c in currObject.GetComponents<Component>())
            {
                var componentType = c.GetType();
                var addedFieldsHeader = false;
                var addedPropertiesHeader = false;
                var addedMethodsHeader = false;

                foreach (var field in componentType.GetRuntimeFields())
                {
                    if (!field.IsPublic) { continue; }

                    var fieldType = field.FieldType;

                    if (!linkValueType.IsAssignableFrom(fieldType)) { continue; }

                    if (!addedFieldsHeader)
                    {
                        menu.AddDisabledItem(new GUIContent($"{componentType.Name}/Fields"));
                        addedFieldsHeader = true;
                    }

                    var current = c == currObject && field.Name == memberProp.stringValue;
                    var label = $"{componentType.Name}/{GetTypeName(fieldType)} {field.Name}";

                    menu.AddItem(new GUIContent(label), current,
                                 () => SelectMember(c, field.Name, Kind.Field, property));
                }

                foreach (var prop in componentType.GetRuntimeProperties())
                {
                    if (!prop.GetGetMethod()?.IsPublic ?? false) { continue; }

                    // MonoBehaviour stuff that is almost certainly not going to be used here
                    if (prop.Name is
                        "allowPrefabModeInPlayMode" or "useGUILayout" or "runInEditMode")
                    {
                        continue;
                    }

                    var propType = prop.PropertyType;

                    if (!linkValueType.IsAssignableFrom(propType)) { continue; }

                    if (!addedPropertiesHeader)
                    {
                        if (addedFieldsHeader) { menu.AddSeparator($"{componentType.Name}/"); }

                        menu.AddDisabledItem(new GUIContent($"{componentType.Name}/Properties"));
                        addedPropertiesHeader = true;
                    }

                    var current = c == currObject && prop.Name == memberProp.stringValue;
                    var label = $"{componentType.Name}/{GetTypeName(propType)} {prop.Name}";

                    menu.AddItem(new GUIContent(label), current,
                                 () => SelectMember(c, prop.Name, Kind.Property, property));
                }

                foreach (var method in componentType.GetRuntimeMethods())
                {
                    var returnType = method.ReturnType;

                    if (!linkValueType.IsAssignableFrom(returnType)) { continue; }
                    if (!method.IsPublic) { continue; }
                    if (method.Name.StartsWith("get_")) { continue; } // property getter
                    if (method.Name is "IsInvoking") { continue; } // MonoBehaviour stuff
                    if (method.GetParameters().Length > 0) { continue; } // needs arguments

                    if (!addedMethodsHeader)
                    {
                        if (addedFieldsHeader || addedPropertiesHeader)
                        {
                            menu.AddSeparator($"{componentType.Name}/");
                        }

                        menu.AddDisabledItem(new GUIContent($"{componentType.Name}/Methods"));
                        addedMethodsHeader = true;
                    }

                    var current = c == currObject && method.Name == memberProp.stringValue;
                    var label = $"{componentType.Name}/{GetTypeName(returnType)} {method.Name}()";

                    menu.AddItem(new GUIContent(label), current,
                                 () => SelectMember(c, method.Name, Kind.Method, property));
                }
            }

            return menu;
        }

        private static readonly Dictionary<Type, string> s_aliases = new()
        {
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(float)] = "float",
            [typeof(double)] = "double",
            [typeof(decimal)] = "decimal",
            [typeof(object)] = "object",
            [typeof(bool)] = "bool",
            [typeof(char)] = "char",
            [typeof(string)] = "string",
            [typeof(void)] = "void"
        };

        private static string GetTypeName(Type type)
            => s_aliases.TryGetValue(type, out var name) ? name : type.Name;
    }
}