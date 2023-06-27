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

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal class OVRConfigurationTaskUpdaterSummary
{
    private readonly List<OVRConfigurationTask> _doneTasks;
    private readonly List<OVRConfigurationTask> _outstandingTasks;
    private readonly Dictionary<OVRProjectSetup.TaskLevel, List<OVRConfigurationTask>> _outstandingTasksPerLevel;
    private bool HasNewOutstandingTasks { get; set; }
    private bool HasAnyChange { get; set; }

    public bool HasAvailableFixes => _outstandingTasks.Count > 0;
    public bool HasFixes(OVRProjectSetup.TaskLevel taskLevel) => _outstandingTasksPerLevel[taskLevel].Count > 0;
    public int GetNumberOfFixes(OVRProjectSetup.TaskLevel taskLevel) => _outstandingTasksPerLevel[taskLevel].Count;
    public int GetTotalNumberOfFixes() => _outstandingTasks.Count;
    private readonly BuildTargetGroup _buildTargetGroup;

    public BuildTargetGroup BuildTargetGroup => _buildTargetGroup;

    public OVRConfigurationTaskUpdaterSummary(BuildTargetGroup buildTargetGroup)
    {
        _buildTargetGroup = buildTargetGroup;
        _doneTasks = new List<OVRConfigurationTask>();
        _outstandingTasks = new List<OVRConfigurationTask>();
        _outstandingTasksPerLevel = new Dictionary<OVRProjectSetup.TaskLevel, List<OVRConfigurationTask>>();
        for (var i = OVRProjectSetup.TaskLevel.Required; i >= OVRProjectSetup.TaskLevel.Optional; i--)
        {
            _outstandingTasksPerLevel.Add(i, new List<OVRConfigurationTask>());
        }
    }

    public void Reset()
    {
        _doneTasks.Clear();
        _outstandingTasks.Clear();
        for (var i = OVRProjectSetup.TaskLevel.Required; i >= OVRProjectSetup.TaskLevel.Optional; i--)
        {
            _outstandingTasksPerLevel[i].Clear();
        }

        HasNewOutstandingTasks = false;
        HasAnyChange = false;
    }

    public void AddTask(OVRConfigurationTask task, bool changedState)
    {
        if (task.IsDone(_buildTargetGroup))
        {
            _doneTasks.Add(task);
        }
        else
        {
            _outstandingTasks.Add(task);
            _outstandingTasksPerLevel[task.Level.GetValue(_buildTargetGroup)].Add(task);
            HasNewOutstandingTasks |= changedState;
        }

        HasAnyChange |= changedState;
    }

    public void Validate()
    {
        if (HasAnyChange)
        {
            LogEvent();
        }

        var interactionFlowEvent = OVRProjectSetupSettingsProvider.InteractionFlowEvent;
        if (interactionFlowEvent == null)
        {
            if (GetTotalNumberOfFixes() > 0)
            {
                interactionFlowEvent = OVRTelemetry
                    .Start(OVRProjectSetupTelemetryEvent.EventTypes.InteractionFlow)
                    .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Level,
                        HighestFixLevel?.ToString() ?? "None")
                    .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Count,
                        GetNumberOfFixes(HighestFixLevel ?? OVRProjectSetup.TaskLevel.Required).ToString())
                    .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Level,
                        HighestFixLevel?.ToString() ?? "None")
                    .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Value,
                        GetTotalNumberOfFixes().ToString())
                    .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.BuildTargetGroup,
                        BuildTargetGroup.ToString());

                OVRProjectSetupSettingsProvider.InteractionFlowEvent = interactionFlowEvent;
            }
        }
        else
        {
            interactionFlowEvent = interactionFlowEvent?.AddAnnotation(
                    OVRProjectSetupTelemetryEvent.AnnotationTypes.BuildTargetGroupAfter,
                    BuildTargetGroup.ToString())
                .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.ValueAfter,
                    GetTotalNumberOfFixes().ToString());

            OVRProjectSetupSettingsProvider.InteractionFlowEvent = interactionFlowEvent;
        }
    }

    public OVRProjectSetup.TaskLevel? HighestFixLevel
    {
        get
        {
            for (var i = OVRProjectSetup.TaskLevel.Required; i >= OVRProjectSetup.TaskLevel.Optional; i--)
            {
                if (HasFixes(i))
                {
                    return i;
                }
            }

            return null;
        }
    }

    public string GenerateReport(string outputPath = null, string fileName = null)
    {
        var sortedTasks = _outstandingTasks.Concat(_doneTasks);
        return OVRProjectSetupReport.GenerateJson(
            sortedTasks,
            _buildTargetGroup,
            outputPath,
            fileName
        );
    }

    public string ComputeNoticeMessage()
    {
        var highestLevel = HighestFixLevel;
        var level = highestLevel ?? OVRProjectSetup.TaskLevel.Optional;
        var count = GetNumberOfFixes(level);
        if (count == 0)
        {
            return $"Oculus-Ready for {_buildTargetGroup}";
        }
        else
        {
            var message = GetLogMessage(level, count);
            return message;
        }
    }

    public string ComputeLogMessage()
    {
        var highestLevel = HighestFixLevel;
        var level = highestLevel ?? OVRProjectSetup.TaskLevel.Optional;
        var count = GetNumberOfFixes(level);
        var message = GetFullLogMessage(level, count);
        return message;
    }

    public void LogEvent()
    {
        OVRTelemetry.Start(OVRProjectSetupTelemetryEvent.EventTypes.Summary)
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Level, HighestFixLevel?.ToString() ?? "None")
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Count,
                GetNumberOfFixes(HighestFixLevel ?? OVRProjectSetup.TaskLevel.Required).ToString())
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.BuildTargetGroup, BuildTargetGroup.ToString())
            .AddAnnotation(OVRProjectSetupTelemetryEvent.AnnotationTypes.Value, GetTotalNumberOfFixes().ToString())
            .Send();
    }


    public void Log()
    {
        if (!HasNewOutstandingTasks)
        {
            return;
        }

        var highestLevel = HighestFixLevel;
        var message = ComputeLogMessage();

        switch (highestLevel)
        {
            case OVRProjectSetup.TaskLevel.Optional:
            {
                Debug.Log(message);
            }
                break;

            case OVRProjectSetup.TaskLevel.Recommended:
            {
                Debug.LogWarning(message);
            }
                break;

            case OVRProjectSetup.TaskLevel.Required:
            {
                if (OVRProjectSetup.RequiredThrowErrors.Value)
                {
                    Debug.LogError(message);
                }
                else
                {
                    Debug.LogWarning(message);
                }
            }
                break;
        }
    }

    private static string GetLogMessage(OVRProjectSetup.TaskLevel level, int count)
    {
        switch (count)
        {
            case 0:
                return $"There are no outstanding {level.ToString()} fixes.";

            case 1:
                return $"There is 1 outstanding {level.ToString()} fix.";

            default:
                return $"There are {count} outstanding {level.ToString()} fixes.";
        }
    }

    internal static string GetFullLogMessage(OVRProjectSetup.TaskLevel level, int count)
    {
        return
            $"[Oculus Settings] {GetLogMessage(level, count)}\nFor more information, go to <a href=\"{OVRConfigurationTask.ConsoleLinkHref}\">Edit > Project Settings > {OVRProjectSetupSettingsProvider.SettingsName}</a>";
    }
}
