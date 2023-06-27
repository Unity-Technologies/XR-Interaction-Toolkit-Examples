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
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class OVRProjectSetupMovementSDKConfigurationTasks
{
    private const OVRProjectSetup.TaskGroup Group = OVRProjectSetup.TaskGroup.Features;

    static OVRProjectSetupMovementSDKConfigurationTasks()
    {
        CheckBodyTrackingTasks();
        CheckFaceTrackingTasks();
    }

    private static void CheckBodyTrackingTasks()
    {
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Required,
            group: Group,
            isDone: buildTargetGroup => FindMisconfiguredOVRSkeletonInstances().Count == 0,
            message: "When using OVRSkeleton components it's required to have OVRBody data provider next to it",
            fix: buildTargetGroup =>
            {
                var skeletons = FindMisconfiguredOVRSkeletonInstances();
                foreach (var skeleton in skeletons)
                {
                    OVRSkeletonEditor.FixOVRBodyConfiguration(skeleton);
                }
            },
            fixMessage: $"Crete OVRBody components where they are required"
        );
    }

    private static void CheckFaceTrackingTasks()
    {
        OVRProjectSetup.AddTask(
            level: OVRProjectSetup.TaskLevel.Required,
            group: Group,
            isDone: buildTargetGroup => FindMisconfiguredOVRCustomFaceInstances().Count == 0,
            message:
            "When using OVRCustomFace components it's required to have OVRFaceExpressions data provider next to it",
            fix: buildTargetGroup =>
            {
                var faces = FindMisconfiguredOVRCustomFaceInstances();
                foreach (var face in faces)
                {
                    OVRCustomFaceEditor.FixFaceExpressions(face);
                }
            },
            fixMessage: $"Crete OVRFaceExpressions components where they are required"
        );
    }

    private static List<OVRSkeleton> FindMisconfiguredOVRSkeletonInstances() => FindComponentsInScene<OVRSkeleton>()
        .FindAll(s => !OVRSkeletonEditor.IsSkeletonProperlyConfigured(s))
        .ToList();

    private static List<OVRCustomFace> FindMisconfiguredOVRCustomFaceInstances() =>
        FindComponentsInScene<OVRCustomFace>()
            .FindAll(s => !OVRCustomFaceEditor.IsFaceExpressionsConfigured(s))
            .ToList();

    private static List<T> FindComponentsInScene<T>() where T : MonoBehaviour
    {
        List<T> results = new List<T>();
        var scene = SceneManager.GetActiveScene();
        var rootGameObjects = scene.GetRootGameObjects();

        foreach (var root in rootGameObjects)
        {
            results.AddRange(root.GetComponentsInChildren<T>());
        }

        return results;
    }
}
