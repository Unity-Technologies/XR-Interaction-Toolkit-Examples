// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using VRBuilder.Core;
using VRBuilder.Editor.Configuration;
using VRBuilder.Editor.Tabs;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Drawer for a step to skip NameableDrawer.
    /// Skip label draw call, as well.
    /// </summary>
    [DefaultProcessDrawer(typeof(Step.EntityData))]
    internal class StepDrawer : ObjectDrawer
    {
        private IStepData lastStep;
        private LockablePropertyTab lockablePropertyTab;

        private static int margin = 3;
        private static int padding = 2;

        protected StepDrawer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        ~StepDrawer()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return rect;
            }

            rect = base.Draw(rect, currentValue, changeValueCallback, label);

            Step.EntityData step = (Step.EntityData) currentValue;

            if (step.Metadata == null)
            {
                step.Metadata = new Metadata();
            }

            if (lastStep != step)
            {
                lockablePropertyTab = new LockablePropertyTab(new GUIContent("Unlocked Objects"), step);
                lastStep = step;
            }

            GUIContent behaviorLabel = new GUIContent("Behaviors");
            if (EditorConfigurator.Instance.Validation.LastReport != null && EditorConfigurator.Instance.Validation.LastReport.GetBehaviorEntriesFor(step).Count > 0)
            {
                behaviorLabel.image = EditorGUIUtility.IconContent("Warning").image;
            }

            GUIContent transitionLabel = new GUIContent("Transitions");
            if (EditorConfigurator.Instance.Validation.LastReport != null && EditorConfigurator.Instance.Validation.LastReport.GetConditionEntriesFor(step).Count > 0)
            {
                transitionLabel.image = EditorGUIUtility.IconContent("Warning").image;
            }

            TabsGroup activeTab = new TabsGroup(
                step.Metadata,
                new DynamicTab(behaviorLabel, () => step.Behaviors, value => step.Behaviors = (IBehaviorCollection)value),
                new DynamicTab(transitionLabel, () => step.Transitions, value => step.Transitions = (ITransitionCollection)value),
                lockablePropertyTab
            );

            Rect tabRect = new TabsGroupDrawer().Draw(new Rect(rect.x, rect.y + rect.height + 4f, rect.width, 0), activeTab, changeValueCallback, label);
            rect.height += tabRect.height;
            return rect;
        }

        protected override float DrawLabel(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            Step.EntityData step = currentValue as Step.EntityData;

            Rect labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            Rect textfieldRect = rect;
            textfieldRect.x += EditorGUIUtility.labelWidth + padding;
            textfieldRect.width -= (EditorGUIUtility.labelWidth + padding);

            GUIStyle textFieldStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 12
            };

            rect.height = labelStyle.CalcHeight(new GUIContent("Step Name"), rect.width) + margin;

            EditorGUI.LabelField(labelRect, "Step Name", labelStyle);

            string oldName = step.Name;
            string newName = EditorGUI.DelayedTextField(textfieldRect, step.Name, textFieldStyle);

            if (newName != step.Name)
            {
                ChangeValue(() =>
                    {
                        step.Name = newName;
                        return step;
                    },
                    () =>
                    {
                        step.Name = oldName;
                        return step;
                    },
                    changeValueCallback);
            }

            return rect.height;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange mode)
        {

        }
    }
}
