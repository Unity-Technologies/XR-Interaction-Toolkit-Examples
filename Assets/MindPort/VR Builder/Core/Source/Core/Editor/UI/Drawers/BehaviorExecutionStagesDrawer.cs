// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2023 MindPort GmbH

using System;
using VRBuilder.Core.Behaviors;
using UnityEditor;
using UnityEngine;

namespace VRBuilder.Editor.UI.Drawers
{
    /// <summary>
    /// Process drawer for <see cref="BehaviorExecutionStages"/> members.
    /// </summary>
    [DefaultProcessDrawer(typeof(BehaviorExecutionStages))]
    internal class BehaviorExecutionStagesDrawer : AbstractDrawer
    {
        private enum ExecutionStages
        {
            BeforeStepExecution = 1 << 0,
            AfterStepExecution = 1 << 1,
            BeforeAndAfterStepExecution = ~0
        }

        /// <inheritdoc />
        public override Rect Draw(Rect rect, object currentValue, Action<object> changeValueCallback, GUIContent label)
        {
            rect.height = EditorDrawingHelper.SingleLineHeight;

            BehaviorExecutionStages oldBehaviorExecutionStages = (BehaviorExecutionStages)currentValue;
            BehaviorExecutionStages newBehaviorExecutionStages;
            ExecutionStages oldExecutionStages;
            ExecutionStages newExecutionStages;

            oldExecutionStages = (ExecutionStages)(int)currentValue;
            newExecutionStages = (ExecutionStages)EditorGUI.EnumPopup(rect, label, oldExecutionStages);

            if (newExecutionStages != oldExecutionStages)
            {
                switch (newExecutionStages)
                {
                    case ExecutionStages.AfterStepExecution:
                        newBehaviorExecutionStages = BehaviorExecutionStages.Deactivation;
                        break;
                    case ExecutionStages.BeforeAndAfterStepExecution:
                        newBehaviorExecutionStages = BehaviorExecutionStages.ActivationAndDeactivation;
                        break;
                    default:
                        newBehaviorExecutionStages = BehaviorExecutionStages.Activation;
                        break;
                }

                ChangeValue(() => newBehaviorExecutionStages, () => oldBehaviorExecutionStages, changeValueCallback);
            }

            return rect;
        }
    }
}
