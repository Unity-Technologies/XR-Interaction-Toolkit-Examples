// Copyright (c) Meta Platforms, Inc. and affiliates.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NorthStar
{
    public class ModelCombiner : AssetPostprocessor
    {
        private const string KEY = "Combine";

        [MenuItem("Assets/Model/Toggle Combine", true)]
        public static bool OnMenuSelectValidate()
        {
            var selection = Selection.activeGameObject;
            return selection != null && PrefabUtility.GetPrefabAssetType(selection) == PrefabAssetType.Model;
        }

        [MenuItem("Assets/Model/Toggle Combine", false)]
        public static void OnMenuSelect()
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Selection.activeObject)) as ModelImporter;

            var properties = importer.extraUserProperties.ToList();
            var isEnabled = properties.Contains(KEY);

            if (isEnabled)
            {
                _ = properties.Remove(KEY);
            }
            else
            {
                properties.Add(KEY);
            }

            importer.extraUserProperties = properties.ToArray();
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Imports the model, checking if it's rotation should be fixed by seeing if it contains a specific key
        /// </summary>
        /// <param name="gameObject"></param>
        public void OnPostprocessModel(GameObject gameObject)
        {
            var importer = assetImporter as ModelImporter;
            if (!importer.extraUserProperties.Contains(KEY))
            {
                return;
            }

            var childMeshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            if (childMeshFilters.Length < 1)
            {
                return;
            }

            var materialCombineInstances = new Dictionary<Material, List<CombineInstance>>();
            var firstMesh = childMeshFilters[0].sharedMesh;

            // Get all meshFilters in children
            foreach (var childMeshFilter in childMeshFilters)
            {
                var mesh = childMeshFilter.sharedMesh;
                if (mesh == null) continue;

                var meshRenderer = childMeshFilter.GetComponent<MeshRenderer>();
                if (meshRenderer == null || !meshRenderer.enabled) continue;

                // Go through materials in meshRenderer
                var sharedMaterials = meshRenderer.sharedMaterials;
                for (var i = 0; i < sharedMaterials.Length; i++)
                {
                    var sharedMaterial = sharedMaterials[i];
                    if (!materialCombineInstances.TryGetValue(sharedMaterial, out var combineInstances))
                    {
                        combineInstances = new List<CombineInstance>();
                        materialCombineInstances.Add(sharedMaterial, combineInstances);
                    }

                    var combineInstance = new CombineInstance
                    {
                        mesh = mesh,
                        transform = childMeshFilter.transform.localToWorldMatrix,
                        subMeshIndex = i
                    };

                    combineInstances.Add(combineInstance);
                }

                var childGameObject = childMeshFilter.gameObject;
                Object.DestroyImmediate(childMeshFilter, true);
                Object.DestroyImmediate(meshRenderer, true);

                if (childGameObject != gameObject)
                {
                    Object.DestroyImmediate(childGameObject, true);
                }
            }

            // Remove any other empty child gameobjects
            DestroyEmptyChildren(gameObject.transform);

            // Combine the CombineInstances into a Combined CombineInstance Mesh that is Combined by Combining CombineInstances
            var finalCombineInstances = new List<CombineInstance>();
            foreach (var data in materialCombineInstances)
            {
                var targetMeshes = data.Value.ToArray();
                var mesh = new Mesh();
                mesh.CombineMeshes(targetMeshes);

                var combineInstance = new CombineInstance
                {
                    mesh = mesh,
                    transform = gameObject.transform.worldToLocalMatrix
                };

                finalCombineInstances.Add(combineInstance);
            }

            // Destroy the source meshes after combining, as some may be used more than once for multiple materials
            foreach (var data in materialCombineInstances)
            {
                foreach (var sourceMesh in data.Value)
                {
                    if (sourceMesh.mesh != null && sourceMesh.mesh != firstMesh)
                    {
                        Object.DestroyImmediate(sourceMesh.mesh, true);
                    }
                }
            }

            // We need to reuse a mesh to store the data, so get the first available mesh
            var result = firstMesh;
            result.name = gameObject.name;
            result.CombineMeshes(finalCombineInstances.ToArray(), false, true);

            var combinedMeshFilter = gameObject.GetComponent<MeshFilter>();
            if (combinedMeshFilter == null) combinedMeshFilter = gameObject.AddComponent<MeshFilter>();
            combinedMeshFilter.sharedMesh = result;

            var combinedMeshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (combinedMeshRenderer == null) combinedMeshRenderer = gameObject.AddComponent<MeshRenderer>();
            combinedMeshRenderer.sharedMaterials = materialCombineInstances.Keys.ToArray();

            if (importer.addCollider)
            {
                if (!gameObject.TryGetComponent<MeshCollider>(out var meshCollider))
                {
                    meshCollider = gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = result;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void DestroyEmptyChildren(Transform transform)
        {
            var childCount = transform.childCount;

            // Iterate backwards, as we destroy children as we go
            for (var i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                DestroyEmptyChildren(child);

                if (child.childCount == 0)
                {
                    Object.DestroyImmediate(child.gameObject, true);
                }
            }
        }
    }
}