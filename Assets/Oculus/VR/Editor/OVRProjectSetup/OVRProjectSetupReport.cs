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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
internal class OVRConfigurationTaskJsonReport
{
    public string uid;
    public string group;
    public string message;
    public string level;
    public bool isDone;
}

[Serializable]
internal class OVRProjectSetupJsonReport
{
    public string createdAt;
    public string buildTargetGroup;
    public string projectName;
    public string unityVersion;
    public string projectUrl;
    public List<OVRConfigurationTaskJsonReport> tasksStatus;
}

internal class OVRProjectSetupReport
{
    public const string ReportDefaultFileNamePrefix = "setuptoolreport";
    public const string ReportDefaultFileNameDateTimeFormat = "yyyy-dd-MM--HH-mm-ss";
    public const string ReportDefaultOutputPath = "./";

    /// <summary>
    /// Generate a JSON report and writes it in a JSON file at the given output path.
    /// </summary>
    /// <param name="tasks">The tasks that will be included in the report.</param>
    /// <param name="buildTargetGroup">Platform for which this report applies.</param>
    /// <param name="outputPath">The path where the report file will be written. If null, the default output path will be used.</param>
    /// <param name="fileName">The name of the generated report file. If null, a name will be generated.</param>
    /// <returns>Returns the full path to the created report file, including the file name.</returns>
    public static string GenerateJson(
        IEnumerable<OVRConfigurationTask> tasks,
        BuildTargetGroup buildTargetGroup,
        string outputPath = null,
        string fileName = null
    )
    {
        var report = BuildSerializableReport(tasks, buildTargetGroup);
        var creationTime = DateTime.Parse(report.createdAt);
        var outputFilePath =
            Path.Combine(outputPath ?? ReportDefaultOutputPath, fileName ?? GenerateFileName(creationTime));
        try
        {
            var jsonString = JsonUtility.ToJson(report, prettyPrint: true);
            File.WriteAllText(outputFilePath, jsonString);
        }
        catch (Exception e)
        {
            throw new Exception("Could not write project setup report", e);
        }

        return outputFilePath;
    }

    private static string GenerateFileName(DateTime creationTime)
    {
        return ReportDefaultFileNamePrefix + "_" + creationTime.ToString(ReportDefaultFileNameDateTimeFormat) + ".json";
    }

    private static OVRProjectSetupJsonReport BuildSerializableReport(
        IEnumerable<OVRConfigurationTask> tasks,
        BuildTargetGroup buildTargetGroup
    )
    {
        var tasksReports = tasks.Select(t => new OVRConfigurationTaskJsonReport
            {
                uid = t.Uid.ToString(),
                group = t.Group.ToString(),
                message = t.Message.GetValue(buildTargetGroup).ToString(),
                level = t.Level.GetValue(buildTargetGroup).ToString(),
                isDone = t.IsDone(buildTargetGroup)
            })
            .ToList();

        var report = new OVRProjectSetupJsonReport
        {
            createdAt = DateTime.Now.ToUniversalTime().ToString(),
            buildTargetGroup = buildTargetGroup.ToString(),
            projectName = PlayerSettings.productName,
            unityVersion = Application.unityVersion,
            projectUrl = RemoveSuffix(Application.dataPath, "/Assets"),
            tasksStatus = tasksReports
        };

        return report;
    }

    private static string RemoveSuffix(string source, string suffix)
    {
        return source.EndsWith(suffix) ? source.Remove(source.LastIndexOf(suffix, StringComparison.Ordinal)) : source;
    }
}
