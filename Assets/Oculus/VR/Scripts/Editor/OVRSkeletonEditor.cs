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

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(OVRSkeleton))]
public class OVRSkeletonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var skeleton = (OVRSkeleton)target;

        if (skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.None)
        {
            EditorGUILayout.HelpBox("Please select a SkeletonType.", MessageType.Warning);
        }

        if (!IsSkeletonProperlyConfigured(skeleton))
        {
            if (OVREditorUIElements.RenderWarningWithButton(
                    "OVRBody is required.", "Add OVRBody component"))
            {
                FixOVRBodyConfiguration(skeleton);
            }
        }

        DrawDefaultInspector();
    }

    internal static bool IsSkeletonProperlyConfigured(OVRSkeleton skeleton)
    {
        return skeleton.GetSkeletonType() != OVRSkeleton.SkeletonType.Body ||
               skeleton.SearchSkeletonDataProvider() != null;
    }

    internal static void FixOVRBodyConfiguration(OVRSkeleton skeleton)
    {
        var gameObject = skeleton.gameObject;
        Undo.IncrementCurrentGroup();
        var body = gameObject.AddComponent<OVRBody>();
        Undo.RegisterCreatedObjectUndo(body, "Add OVRBody component");
        EditorUtility.SetDirty(body);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Undo.SetCurrentGroupName("Add OVRBody component");
    }
}
