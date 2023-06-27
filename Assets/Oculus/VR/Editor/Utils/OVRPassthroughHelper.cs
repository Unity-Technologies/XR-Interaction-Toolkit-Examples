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
using UnityEditor.SceneManagement;
using UnityEngine;

internal class OVRPassthroughHelper
{
    private const string OVRCameraRigPrefabPath = "Assets/Oculus/VR/Prefabs/OVRCameraRig.prefab";

#if UNITY_2021_1_OR_NEWER
    private static IEnumerable<GameObject> FindAllInstancesOfPrefab(GameObject prefab) =>
        PrefabUtility.FindAllInstancesOfPrefab(prefab);
#else
    private static IEnumerable<GameObject> FindAllInstancesOfPrefab(GameObject prefab) =>
        GameObject.FindObjectsOfType(typeof(GameObject))
            .OfType<GameObject>()
            .Where(o => PrefabUtility.GetPrefabAssetType(o) == PrefabAssetType.Regular &&
                        PrefabUtility.GetCorrespondingObjectFromSource(o) == prefab).ToList();
#endif

    internal GameObject GetOvrCameraRig()
    {
        var ovrCameraRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OVRCameraRigPrefabPath);

        if (ovrCameraRigPrefab is null)
        {
            return null;
        }

        var ovrCameraRig = FindAllInstancesOfPrefab(ovrCameraRigPrefab).FirstOrDefault();
        return ovrCameraRig;
    }

    internal bool EnablePassthrough(GameObject ovrCameraRig)
    {
        var ovrManager = ovrCameraRig.GetComponent<OVRManager>();

        if (ovrManager is null)
        {
            return false;
        }

        ovrManager.isInsightPassthroughEnabled = true;
        return true;
    }

    internal bool IsAnyPassthroughLayerUnderlay(GameObject ovrCameraRig)
    {
        var passthroughLayers = ovrCameraRig.GetComponents<OVRPassthroughLayer>();
        return passthroughLayers.Any(p => p.overlayType == OVROverlay.OverlayType.Underlay);
    }

    internal bool InitPassthroughLayerUnderlay(GameObject ovrCameraRig)
    {
        var passthroughLayers = ovrCameraRig.GetComponents<OVRPassthroughLayer>();

        // no PT layers
        if (!passthroughLayers.Any())
        {
            var underLay = ovrCameraRig.AddComponent<OVRPassthroughLayer>();
            underLay.overlayType = OVROverlay.OverlayType.Underlay;
        }
        // there are layers but non of them are Underlay
        else if (!passthroughLayers.Any(l => l.overlayType == OVROverlay.OverlayType.Underlay))
        {
            // if there is only one PT layer, change it to Underlay
            if (passthroughLayers.Length == 1)
            {
                passthroughLayers.First().overlayType = OVROverlay.OverlayType.Underlay;
            }
            else
            {
                Debug.LogError(
                    "There are multiple OVRPassthroughLayer instances in the scene, but none of them is an Underlay. Set one of the layer's Placement to Underlay.");
                return false;
            }
        }

        SaveScene();
        return true;
    }

    internal bool HasCentralCamera(GameObject ovrCameraRig) =>
        ovrCameraRig.GetComponent<OVRCameraRig>()?.centerEyeAnchor.GetComponent<Camera>() != null;

    internal bool IsBackgroundClear(GameObject ovrCameraRig)
    {
        var centerCamera = ovrCameraRig.GetComponent<OVRCameraRig>()?.centerEyeAnchor.GetComponent<Camera>();

        if (centerCamera is null)
        {
            throw new System.Exception("Central camera was not found");
        }

        return centerCamera.clearFlags == CameraClearFlags.SolidColor && centerCamera.backgroundColor.a < 1;
    }

    internal void ClearBackgroud(GameObject ovrCameraRig)
    {
        var centerCamera = ovrCameraRig.GetComponent<OVRCameraRig>()?.centerEyeAnchor.GetComponent<Camera>();

        if (centerCamera is null)
        {
            throw new System.Exception("Central camera was not found");
        }

        centerCamera.clearFlags = CameraClearFlags.SolidColor;
        centerCamera.backgroundColor = Color.clear;

        SaveScene();
    }

    private void SaveScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
    }
}
