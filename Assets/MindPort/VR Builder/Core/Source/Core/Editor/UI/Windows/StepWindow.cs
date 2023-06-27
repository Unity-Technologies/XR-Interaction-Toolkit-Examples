// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRBuilder.Core;
using VRBuilder.Editor.Tabs;
using VRBuilder.Editor.UI.Drawers;
using VRBuilder.Editor.Configuration;

namespace VRBuilder.Editor.UI.Windows
{
    /// <summary>
    /// This class draws the Step Inspector.
    /// </summary>
    internal class StepWindow : EditorWindow, IStepView
    {
        private const int border = 4;

        private IStep step;

        [SerializeField]
        private Vector2 scrollPosition;

        [SerializeField]
        private Rect stepRect;

        /// <summary>
        /// Returns the first <see cref="StepWindow"/> which is currently opened.
        /// If there is none, creates and shows <see cref="StepWindow"/>.
        /// </summary>
        public static void ShowInspector()
        {
            StepWindow window = GetInstance();
            window.Repaint();
            window.Focus();
        }

        public static StepWindow GetInstance(bool focus = false)
        {
            return GetWindow<StepWindow>("Step Inspector", focus);
        }

        private void OnEnable()
        {
            GlobalEditorHandler.StepWindowOpened(this);
        }

        private void OnDestroy()
        {
            GlobalEditorHandler.StepWindowClosed(this);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnFocus()
        {
            if (step?.Data == null)
            {
                return;
            }

            if (EditorConfigurator.Instance.Validation.IsAllowedToValidate())
            {
                EditorConfigurator.Instance.Validation.Validate(step.Data, GlobalEditorHandler.GetCurrentProcess());
            }
        }

        private void OnGUI()
        {
            if (step == null)
            {
                return;
            }

            IProcessDrawer drawer = DrawerLocator.GetDrawerForValue(step, typeof(Step));

            stepRect.width = position.width;

            if (stepRect.height > position.height)
            {
                stepRect.width -= GUI.skin.verticalScrollbar.fixedWidth;
            }

            scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), scrollPosition, stepRect, false, false);
            {
                Rect stepDrawingRect = new Rect(stepRect.position + new Vector2(border, border), stepRect.size - new Vector2(border * 2f, border * 2f));
                stepDrawingRect = drawer.Draw(stepDrawingRect, step, ModifyStep, "Step");
                stepRect = new Rect(stepDrawingRect.position - new Vector2(border,border), stepDrawingRect.size + new Vector2(border * 2f, border * 2f));
            }
            GUI.EndScrollView();
        }

        private void ModifyStep(object newStep)
        {
            step = (IStep)newStep;
            GlobalEditorHandler.CurrentStepModified(step);
        }

        public void SetStep(IStep newStep)
        {
            step = newStep;
        }

        public IStep GetStep()
        {
            return step;
        }

        public void ResetStepView()
        {
            if (EditorUtils.IsWindowOpened<StepWindow>() == false || step == null)
            {
                return;
            }

            Dictionary<string, object> dict = step.Data.Metadata.GetMetadata(typeof(TabsGroup));
            if (dict.ContainsKey(TabsGroup.SelectedKey))
            {
                dict[TabsGroup.SelectedKey] = 0;
            }
        }
    }
}
