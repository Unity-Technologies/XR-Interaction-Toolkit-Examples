// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Utilities.Editor
{
    public class ResaveAsset
    {
        private static IEnumerable<Scene> GetActiveScenes()
        {
            for (var i = 0; i != SceneManager.sceneCount; i += 1)
                yield return SceneManager.GetSceneAt(i);
        }

        [MenuItem("Assets/Resave")]
        public static async void ResaveAssets()
        {
            try
            {
                _ = EditorUtility.DisplayCancelableProgressBar("Resaving assets", "", 0);

                var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
                var additionalPaths = AssetDatabase.FindAssets("", paths).Select(AssetDatabase.GUIDToAssetPath);
                var assets = paths.Concat(additionalPaths).Distinct().ToArray();
                foreach (var (i, assetPath) in assets.Enumerate())
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Resaving assets", assetPath, i * 1.0f / assets.Length))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    var isPrefab = assetPath.EndsWith("prefab");
                    var objs = isPrefab || assetPath.EndsWith("unity") ? new[] { AssetDatabase.LoadMainAssetAtPath(assetPath) } : AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    foreach (var obj in objs)
                    {
                        if (obj != null)
                        {
                            EditorUtility.SetDirty(obj);
                        }

                        if (obj is SceneAsset sceneAsset)
                        {
                            var scene = GetActiveScenes().FirstOrDefault(s => s.path == assetPath);
                            var wasValid = scene.IsValid();
                            var wasLoaded = scene.isLoaded;
                            if (!wasLoaded)
                            {
                                scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                            }

                            foreach (var rootObj in scene.GetRootGameObjects())
                            {
                                foreach (var comp in rootObj.GetComponentsInChildren<Component>(true))
                                {
                                    EditorUtility.SetDirty(comp);
                                }
                            }
                            _ = EditorSceneManager.MarkSceneDirty(scene);
                            _ = EditorSceneManager.SaveScene(scene);
                            _ = EditorSceneManager.SaveOpenScenes();

                            if (!wasLoaded)
                            {
                                _ = EditorSceneManager.CloseScene(scene, !wasValid);
                            }

                            EditorUtility.SetDirty(sceneAsset);
                        }

                        if (obj is GameObject prefab)
                        {
                            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab) is > 0 and var num)
                            {
                                Debug.LogWarning($"Removed {num} scripts from {prefab}", prefab);
                            }

                            if (isPrefab)
                            {
                                _ = PrefabUtility.SavePrefabAsset(prefab);
                            }
                        }

                        if (obj != null)
                        {
                            AssetDatabase.SaveAssetIfDirty(obj);
                        }

                        await Task.Yield();
                    }
                }

                _ = EditorUtility.DisplayCancelableProgressBar("Resaving assets", "Saving...", 1);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Assets/Resave Assets", true)]
        public static bool ResaveAssetsValid() => Selection.assetGUIDs.Any();
    }
}
