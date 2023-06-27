// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Utils;
using VRBuilder.Editor.Configuration;
using VRBuilder.Editor.UI.StepInspector.Menu;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// Window that allows to manage the allowed menu items.
    /// </summary>
    internal class AllowedMenuItemsWindow : EditorWindow
    {
        private static AllowedMenuItemsWindow window;

        private const string menuPath = "Tools/VR Builder/Developer/Allowed Menu Items Configuration";

        private bool isEditUnlocked;
        private static IList<EntityEntry> behaviorList;
        private static IList<EntityEntry> conditionList;

        private static Texture2D logo = LogoEditorHelper.GetProductLogoTexture(LogoStyle.TopBottom);

        private Vector2 scrollPosition = Vector2.zero;

        private struct EntityEntry
        {
            public string AssemblyQualifiedName;
            public string DisplayedName;
            public bool IsTypeValid;
        }

        [MenuItem(menuPath, false, 80)]
        private static void ShowWindow()
        {
            if (EditorUtils.IsWindowOpened<AllowedMenuItemsWindow>())
            {
                window = Resources.FindObjectsOfTypeAll<AllowedMenuItemsWindow>().First();
            }
            else
            {
                window = GetWindow<AllowedMenuItemsWindow>();
            }

            window.Show();
            window.Focus();
        }

        private static void InitializeLists()
        {
            behaviorList = new List<EntityEntry>();

            foreach (KeyValuePair<string, bool> behavior in EditorConfigurator.Instance.AllowedMenuItemsSettings.SerializedBehaviorSelections)
            {
                Type type = ReflectionUtils.GetTypeFromAssemblyQualifiedName(behavior.Key);
                bool isTypeValid = type != null;
                string displayedName = (isTypeValid) ? ((MenuItem<IBehavior>)ReflectionUtils.CreateInstanceOfType(type)).DisplayedName : behavior.Key;

                behaviorList.Add(new EntityEntry()
                {
                    AssemblyQualifiedName = behavior.Key,
                    DisplayedName = displayedName,
                    IsTypeValid = isTypeValid
                });
                behaviorList = behaviorList
                    .OrderByDescending(entry => entry.IsTypeValid)
                    .ThenBy(entry => entry.DisplayedName, new SortingUtils.AlphaNumericNaturalSortComparer())
                    .ToList();
            }

            conditionList = new List<EntityEntry>();

            foreach (KeyValuePair<string, bool> condition in EditorConfigurator.Instance.AllowedMenuItemsSettings.SerializedConditionSelections)
            {
                Type type = ReflectionUtils.GetTypeFromAssemblyQualifiedName(condition.Key);
                bool isTypeValid = type != null;
                string displayedName = (isTypeValid) ? ((MenuItem<ICondition>)ReflectionUtils.CreateInstanceOfType(type)).DisplayedName : condition.Key;

                conditionList.Add(new EntityEntry()
                {
                    AssemblyQualifiedName = condition.Key,
                    DisplayedName = displayedName,
                    IsTypeValid = isTypeValid
                });
                conditionList = conditionList
                    .OrderByDescending(entry => entry.IsTypeValid)
                    .ThenBy(entry => entry.DisplayedName, new SortingUtils.AlphaNumericNaturalSortComparer())
                    .ToList();
            }
        }

        private void OnGUI()
        {
            // Magic number.
            minSize = new Vector2(420f, 720f);
            titleContent = new GUIContent("Menu Items");

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            if (behaviorList == null || conditionList == null)
            {
                InitializeLists();
            }

            GUILayout.Space(20f);

            Rect rect = GUILayoutUtility.GetRect(position.width, 150, GUI.skin.box);
            GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

            GUILayout.Space(20f);

            EditorGUILayout.HelpBox("This window provides editor configuration settings for VR Builder. "
                + "It is supposed to be changed only by a software developer. "
                + "Modification of these values may lead to various issues.", MessageType.Warning);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (isEditUnlocked == false)
                {
                    if (GUILayout.Button("I understand the risks. Let me edit it."))
                    {
                        isEditUnlocked = true;
                    }
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20f);

            if (string.IsNullOrEmpty(EditorConfigurator.Instance.AllowedMenuItemsSettingsAssetPath))
            {
                EditorGUILayout.LabelField("The property <i>AllowedMenuItemsSettingsAssetPath</i> of the current editor configuration returns <i>null</i> or an empty string. " +
                    "For this reason, this feature is disabled.\n\n" +
                    "If you want to configure what menu items should be displayed in the <i>Process Step Inspector</i>, " +
                    "override the <i>DefaultEditorConfiguration</i> (if you have not already done so) " +
                    "and let this property return a valid asset path.\n\n" +
                    "Example of a valid asset path:\n<i>Assets/Editor/Builder/allowed_menu_items_config.json</i>\n\n" +
                    "Note: The file will then be created at this location and is used to save the configurations made in this window.", labelStyle);

                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, true, true);
            {
                EditorGUI.BeginDisabledGroup(isEditUnlocked == false);
                {
                    EditorGUILayout.LabelField("<b>Here you can change whether a behavior or a condition menu item is selectable in the <i>Process Step Inspector</i></b>.\n\n" +
                        "Non-editable entries are behavior or condition menu items that had been deleted from the project at some point.\n\n" +
                        "<i>Available Behaviors:</i>\n", labelStyle);

                    IDictionary<string, bool> behaviors = EditorConfigurator.Instance.AllowedMenuItemsSettings.SerializedBehaviorSelections;

                    // ReSharper disable once PossibleNullReferenceException
                    if (behaviorList.Count == 0)
                    {
                        EditorGUILayout.LabelField("<i>None</i>", labelStyle);
                    }
                    else
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (EntityEntry entry in behaviorList)
                        {
                            EditorGUI.BeginDisabledGroup(entry.IsTypeValid == false);
                            {
                                if (EditorGUILayout.ToggleLeft(entry.DisplayedName, behaviors[entry.AssemblyQualifiedName]) != behaviors[entry.AssemblyQualifiedName])
                                {
                                    // Toggle was clicked.
                                    behaviors[entry.AssemblyQualifiedName] = behaviors[entry.AssemblyQualifiedName] == false;
                                    AllowedMenuItemsSettings.Save(EditorConfigurator.Instance.AllowedMenuItemsSettings);
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                    }

                    EditorGUILayout.LabelField("\n<i>Available Conditions:</i>\n", labelStyle);

                    IDictionary<string, bool> conditions = EditorConfigurator.Instance.AllowedMenuItemsSettings.SerializedConditionSelections;

                    // ReSharper disable once PossibleNullReferenceException
                    if (conditionList.Count == 0)
                    {
                        EditorGUILayout.LabelField("<i>None</i>", labelStyle);
                    }
                    else
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        foreach (EntityEntry entry in conditionList)
                        {
                            EditorGUI.BeginDisabledGroup(entry.IsTypeValid == false);
                            {
                                if (EditorGUILayout.ToggleLeft(entry.DisplayedName, conditions[entry.AssemblyQualifiedName]) != conditions[entry.AssemblyQualifiedName])
                                {
                                    // Toggle was clicked.
                                    conditions[entry.AssemblyQualifiedName] = conditions[entry.AssemblyQualifiedName] == false;
                                    AllowedMenuItemsSettings.Save(EditorConfigurator.Instance.AllowedMenuItemsSettings);
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndScrollView();
        }
    }
}
