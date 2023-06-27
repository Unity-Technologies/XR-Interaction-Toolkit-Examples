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
using System.IO;
using UnityEditor;

/// <summary>
/// Static class for the OVRProjectSetup Tool that contains methods intended to be called from the CLI
/// </summary>
public class OVRProjectSetupCLI
{
    /// <summary>
    /// Generate a project setup report and write it to a file.
    /// </summary>
    /// <remarks>
    /// This generates a project setup report for the active platform build target and write it to a file.
    /// The active platform build target may be specified using the "-buildTarget" CLI argument.
    /// The output file can be specified using the "-reportFile" CLI argument. If not specified, a
    /// file with a generated name will be created in the current folder.
    /// </remarks>
    public static void GenerateProjectSetupReport()
    {
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

        var reportFile = GetArgValue("-reportFile");
        var outputFolder = reportFile != null ? Path.GetDirectoryName(reportFile) : "./";
        var outputFile = Path.GetFileName(reportFile);

        OVRProjectSetup.UpdateTasks(buildTargetGroup, logMessages: OVRProjectSetup.LogMessages.Disabled, blocking: true,
            onCompleted: processor =>
            {
                var updater = processor as OVRConfigurationTaskUpdater;
                updater?.Summary.GenerateReport(outputFolder, outputFile);
            });
    }

    private static string GetArgValue(string argName)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == argName)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
