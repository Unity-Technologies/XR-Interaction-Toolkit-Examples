// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_NETCODE_GAMEOBJECTS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Utilities.Editor
{
    internal class NetcodeHashFixer : AssetPostprocessor
    {
        [MenuItem("Assets/Regenerate Network Hash Ids")]
        public static async void RegenerateAssetNetworkHashIds()
        {
            var paths = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var assets = Selection.assetGUIDs.Concat(AssetDatabase.FindAssets("", paths));
            foreach (var asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                await FixIdHashForPrefabByPath(path);
            }
        }

        private static async Task FixIdHashForPrefabByPath(string path)
        {
            using var edit = new PrefabUtility.EditPrefabContentsScope(path);
            var objects = edit.prefabContentsRoot.GetComponentsInChildren<NetworkObject>(true);
            foreach (var obj in objects)
            {
                _ = FixIdHash(obj);

                await Task.Yield();
            }
        }

        private static bool FixIdHash(NetworkObject obj)
        {
            var old = obj.GlobalObjectIdHash;
            obj.GenerateGlobalObjectIdHash();

            EditorUtility.SetDirty(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            _ = EditorSceneManager.MarkSceneDirty(obj.gameObject.scene);

            return obj.GlobalObjectIdHash != old;
        }

        private static IEnumerable<Scene> GetActiveScenes()
        {
            for (var i = 0; i != SceneManager.sceneCount; i += 1)
                yield return SceneManager.GetSceneAt(i);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var types = importedAssets.Select(AssetDatabase.GetMainAssetTypeAtPath);
            foreach (var (path, type) in importedAssets.Zip(types))
            {
                if (typeof(SceneAsset).IsAssignableFrom(type))
                {
                    CheckScene(path);
                }
            }
        }

        private static void CheckScene(string path)
        {
            var scene = GetActiveScenes().FirstOrDefault(s => s.path == path);
            var wasValid = scene.IsValid();
            var wasLoaded = scene.isLoaded;

            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            var objects = scene.GetRootGameObjects().
                SelectMany(go => go.GetComponentsInChildren<NetworkObject>(true));
            var anyChanges = false;
            foreach (var obj in objects)
            {
                anyChanges = FixIdHash(obj) || anyChanges;
            }
            anyChanges = RemoveOldPrefabModifications(in scene) || anyChanges;

            if (anyChanges)
            {
                _ = EditorSceneManager.MarkSceneDirty(scene);
                _ = EditorSceneManager.SaveScene(scene);
            }

            if (!wasLoaded)
            {
                _ = EditorSceneManager.CloseScene(scene, !wasValid);
            }
        }

        private static bool RemoveOldPrefabModifications(in Scene scene)
        {
            var anyChanges = false;
            foreach (var instance in scene.
                GetRootGameObjects().
                SelectMany(go => go.GetComponentsInChildren<Transform>(true)).
                Where(t => PrefabUtility.GetPrefabInstanceHandle(t.gameObject)))
            {
                var mods = PrefabUtility.GetPropertyModifications(instance.gameObject);
                var cleanedMods = mods.Where(m => m.target != null).ToArray();
                if (cleanedMods.Length != mods.Length)
                {
                    PrefabUtility.SetPropertyModifications(instance, cleanedMods);
                    anyChanges = true;
                }
            }
            return anyChanges;
        }
    }
}

#endif
