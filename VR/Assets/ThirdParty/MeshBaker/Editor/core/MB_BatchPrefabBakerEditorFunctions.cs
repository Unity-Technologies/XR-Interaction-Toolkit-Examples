using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DigitalOpus.MB.Core;
using System.Text.RegularExpressions;

namespace DigitalOpus.MB.MBEditor
{
    public class MB_BatchPrefabBakerEditorFunctions
    {

        public static int EvalVersionPrefabLimit
        {
            get { return 5; }
        }

        public class UnityTransform
        {
            public Vector3 p;
            public Quaternion q;
            public Vector3 s;
            public Transform t;

            public UnityTransform(Transform t)
            {
                this.t = t;
                p = t.localPosition;
                q = t.localRotation;
                s = t.localScale;
            }
        }

        private enum TargetMeshTreatment
        {
            createNewMesh,
            replaceMesh,
            reuseMesh
        }

        private class ProcessedMeshInfo
        {
            public Material[] srcMaterials;
            public Material[] targMaterials;
            public Mesh targetMesh;
        }

        public static void PopulatePrefabRowsFromTextureBaker(MB3_BatchPrefabBaker prefabBaker)
        {
            MB3_TextureBaker texBaker = prefabBaker.GetComponent<MB3_TextureBaker>();
            List<GameObject> newPrefabs = new List<GameObject>();
            List<GameObject> gos = texBaker.GetObjectsToCombine();
            for (int i = 0; i < gos.Count; i++)
            {
                GameObject go = (GameObject) MBVersionEditor.PrefabUtility_FindPrefabRoot(gos[i]);
                UnityEngine.Object obj = MBVersionEditor.PrefabUtility_GetCorrespondingObjectFromSource(go);

                if (obj != null && obj is GameObject)
                {
                    if (!newPrefabs.Contains((GameObject)obj)) newPrefabs.Add((GameObject)obj);
                }
                else
                {
                    Debug.LogWarning(String.Format("Object {0} did not have a prefab", gos[i]));
                }

            }

            // Remove prefabs that are already in the list of batch prefab baker's prefabs.
            {
                List<GameObject> tmpNewPrefabs = new List<GameObject>();
                for (int i = 0; i < newPrefabs.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < prefabBaker.prefabRows.Length; j++)
                    {
                        if (prefabBaker.prefabRows[j].sourcePrefab == newPrefabs[i])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        tmpNewPrefabs.Add(newPrefabs[i]);
                    }
                }

                newPrefabs = tmpNewPrefabs;
            }

            List<MB3_BatchPrefabBaker.MB3_PrefabBakerRow> newRows = new List<MB3_BatchPrefabBaker.MB3_PrefabBakerRow>();
            if (prefabBaker.prefabRows == null) prefabBaker.prefabRows = new MB3_BatchPrefabBaker.MB3_PrefabBakerRow[0];
            newRows.AddRange(prefabBaker.prefabRows);
            for (int i = 0; i < newPrefabs.Count; i++)
            {
                MB3_BatchPrefabBaker.MB3_PrefabBakerRow row = new MB3_BatchPrefabBaker.MB3_PrefabBakerRow();
                row.sourcePrefab = newPrefabs[i];
                newRows.Add(row);
            }


            Undo.RecordObject(prefabBaker, "Populate prefab rows");
            prefabBaker.prefabRows = newRows.ToArray();
        }


        public static void BakePrefabs(MB3_BatchPrefabBaker pb, bool doReplaceTargetPrefab)
        {
            if (pb.LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("Batch baking prefabs");

            if (MB3_MeshCombiner.EVAL_VERSION)
            {
                int numPrefabsLimit = EvalVersionPrefabLimit;
                if (pb.prefabRows.Length > numPrefabsLimit)
                {
                    Debug.LogError("The free version of mesh baker is limited to batch baking " + numPrefabsLimit + 
                        " prefabs. The full version has no limit on the number of prefabs that can be baked. Delete the extra prefab rows before baking.");
                    return;
                }
            }

            if (Application.isPlaying)
            {
                Debug.LogError("The BatchPrefabBaker cannot be run in play mode.");
                return;
            }

            MB3_MeshBaker mb = pb.GetComponent<MB3_MeshBaker>();
            if (mb == null)
            {
                Debug.LogError("Prefab baker needs to be attached to a Game Object with a MB3_MeshBaker component.");
                return;
            }

            if (mb.textureBakeResults == null)
            {
                Debug.LogError("Texture Bake Results is not set");
                return;
            }

            int numResultMats = mb.textureBakeResults.NumResultMaterials();
            for (int i = 0; i < numResultMats; i++)
            {
                if (mb.textureBakeResults.GetCombinedMaterialForSubmesh(i) == null)
                {
                    Debug.LogError("The texture bake result had a null result material on submesh " + i + ". Try re-baking textures");
                    return;
                }
            }

            if (mb.meshCombiner.outputOption != MB2_OutputOptions.bakeMeshAssetsInPlace)
            {
                mb.meshCombiner.outputOption = MB2_OutputOptions.bakeMeshAssetsInPlace;
            }

            MB2_TextureBakeResults tbr = mb.textureBakeResults;

            HashSet<Mesh> sourceMeshes = new HashSet<Mesh>();
            HashSet<Mesh> allResultMeshes = new HashSet<Mesh>();

            //validate prefabs
            for (int i = 0; i < pb.prefabRows.Length; i++)
            {
                if (pb.prefabRows[i] == null || pb.prefabRows[i].sourcePrefab == null)
                {
                    Debug.LogError("Source Prefab on row " + i + " is not set.");
                    return;
                }
                if (pb.prefabRows[i].resultPrefab == null)
                {
                    Debug.LogError("Result Prefab on row " + i + " is not set.");
                    return;
                }
                for (int j = i + 1; j < pb.prefabRows.Length; j++)
                {
                    if (pb.prefabRows[i].sourcePrefab == pb.prefabRows[j].sourcePrefab)
                    {
                        Debug.LogError("Rows " + i + " and " + j + " contain the same source prefab");
                        return;
                    }
                }
                for (int j = 0; j < pb.prefabRows.Length; j++)
                {
                    if (pb.prefabRows[i].sourcePrefab == pb.prefabRows[j].resultPrefab)
                    {
                        Debug.LogError("Row " + i + " source prefab is the same as row " + j + " result prefab");
                        return;
                    }
                }
                if (MBVersionEditor.PrefabUtility_GetPrefabType(pb.prefabRows[i].sourcePrefab) != MB_PrefabType.modelPrefabAsset &&
                    MBVersionEditor.PrefabUtility_GetPrefabType(pb.prefabRows[i].sourcePrefab) != MB_PrefabType.prefabAsset)
                {
                    Debug.LogError("Row " + i + " source prefab is not a prefab asset ");
                    return;
                }
                if (MBVersionEditor.PrefabUtility_GetPrefabType(pb.prefabRows[i].resultPrefab) != MB_PrefabType.modelPrefabAsset &&
                    MBVersionEditor.PrefabUtility_GetPrefabType(pb.prefabRows[i].resultPrefab) != MB_PrefabType.prefabAsset)
                {
                    Debug.LogError("Row " + i + " result prefab is not a prefab asset");
                    return;
                }

                GameObject so = (GameObject) GameObject.Instantiate(pb.prefabRows[i].sourcePrefab);
                GameObject ro = (GameObject) GameObject.Instantiate(pb.prefabRows[i].resultPrefab);
                Renderer[] rs = (Renderer[])so.GetComponentsInChildren<Renderer>(true);

                for (int j = 0; j < rs.Length; j++)
                {
                    if (IsGoodToBake(rs[j], tbr))
                    {
                        sourceMeshes.Add(MB_Utility.GetMesh(rs[j].gameObject));
                    }
                }
                rs = ro.GetComponentsInChildren<Renderer>(true);

                for (int j = 0; j < rs.Length; j++)
                {
                    Renderer r = rs[j];
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    {
                        Mesh m = MB_Utility.GetMesh(r.gameObject);
                        if (m != null)
                        {
                            allResultMeshes.Add(m);
                        }
                    }
                }
                
                GameObject.DestroyImmediate(so); //todo should cache these and have a proper cleanup at end
                GameObject.DestroyImmediate(ro);
            }

            sourceMeshes.IntersectWith(allResultMeshes);
            HashSet<Mesh> sourceMeshesThatAreUsedByResult = sourceMeshes;
            if (sourceMeshesThatAreUsedByResult.Count > 0)
            {
                foreach (Mesh m in sourceMeshesThatAreUsedByResult)
                {
                    Debug.LogWarning("Mesh " + m + " is used by both the source and result prefabs. New meshes will be created.");
                }
                //return;
            }

            List<UnityTransform> unityTransforms = new List<UnityTransform>();
            // Bake the meshes using the meshBaker component one prefab at a time
            // Version check
            int majorVersion = MBVersion.GetMajorVersion();
            int minorVersion = MBVersion.GetMinorVersion();
            for (int prefabIdx = 0; prefabIdx < pb.prefabRows.Length; prefabIdx++)
            {
                if (majorVersion > 2018 || (majorVersion == 2018 && minorVersion >= 3)) // 2018.3+
                {
                    if (doReplaceTargetPrefab)
                    {
                        _ProcessPrefabRowReplaceTargetPrefab(pb, pb.prefabRows[prefabIdx], tbr, unityTransforms, mb);
                    }
                    else
                    {
                        _ProcessPrefabRowOnlyMeshesAndMaterials(pb, pb.prefabRows[prefabIdx], tbr, unityTransforms, mb);
                    }
                }
                else // 2018.2 and older
                {                   
                    if (doReplaceTargetPrefab)
                    {
                        _ProcessPrefabRowReplaceTargetPrefab_Legacy_2018_2_AndOlder(pb, pb.prefabRows[prefabIdx], tbr, unityTransforms, mb);
                    }
                    else
                    {
                        _ProcessPrefabRowOnlyMeshesAndMaterials_Legacy_2018_2_AndOlder(pb, pb.prefabRows[prefabIdx], tbr, unityTransforms, mb);
                    }
                }
            }

            AssetDatabase.Refresh();
            mb.ClearMesh();
        }

        internal static void _ProcessPrefabRowOnlyMeshesAndMaterials(MB3_BatchPrefabBaker pb, MB3_BatchPrefabBaker.MB3_PrefabBakerRow pr, MB2_TextureBakeResults tbr, List<UnityTransform> unityTransforms, MB3_MeshBaker mb)
        {
            if (pb.LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("==== Processing Source Prefab " + pr.sourcePrefab);

            GameObject srcPrefab = pr.sourcePrefab;
            GameObject targetPrefab = pr.resultPrefab;
            string targetPrefabName = AssetDatabase.GetAssetPath(targetPrefab);
            GameObject srcPrefabInstance = GameObject.Instantiate(srcPrefab);
            Renderer[] rs = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            if (rs.Length < 1)
            {
                Debug.LogWarning("Prefab " + pr.sourcePrefab + " does not have a renderer");
                GameObject.DestroyImmediate(srcPrefabInstance);
                return;
            }

            GameObject targPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab);
            Renderer[] sourceRenderers = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            Dictionary<Mesh, List<ProcessedMeshInfo>> processedMeshesSrcToTargetMap = new Dictionary<Mesh, List<ProcessedMeshInfo>>();

            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (!IsGoodToBake(sourceRenderers[i], tbr))
                {
                    continue;
                }

                Mesh sourceMesh = MB_Utility.GetMesh(sourceRenderers[i].gameObject);

                if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("== Visiting renderer: " + sourceRenderers[i]);
                // Try to find an existing mesh in the target that we can re-use
                Mesh targetMeshAsset = null;
                Transform tr = FindCorrespondingTransform(srcPrefabInstance.transform, sourceRenderers[i].transform, targPrefabInstance.transform);
                Renderer targetRenderer = null;
                if (tr != null)
                {
                    Mesh targMesh = MB_Utility.GetMesh(tr.gameObject);
                    targetRenderer = tr.GetComponent<Renderer>();
                    if (sourceRenderers[i].GetType() == targetRenderer.GetType())
                    {
                        if (AssetDatabase.GetAssetPath(targMesh) == AssetDatabase.GetAssetPath(targetPrefab))
                        {
                            // only replace meshes if they are part of the targetPrefab
                            targetMeshAsset = MB_Utility.GetMesh(tr.gameObject);
                        }

                        if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found corresponding transform in target prefab: " + tr + " mesh: " + targetMeshAsset);
                    }
                    else
                    {
                        Debug.LogError("The renderer on the target prefab matching source prefab " + pr.sourcePrefab + " was not the same kind of renderer " + sourceRenderers[i].name);
                        continue;
                    }
                }
                else
                {
                    Debug.LogError("There was a renderer in source prefab " + pr.sourcePrefab + " that could not be a matched to a target renderer in the target prefab: " + sourceRenderers[i].name +
                        " This can happen if the hierarchy in the source prefab is different from the hierarchy in the target prefab.");
                    continue;
                }

                // Check that we haven't processed this mesh already.
                List<ProcessedMeshInfo> lpmi;
                if (processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi))
                {
                    Material[] srcMats = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                    for (int j = 0; j < lpmi.Count; j++)
                    {
                        if (ComapreMaterials(srcMats, lpmi[j].srcMaterials))
                        {
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found already processed mesh that uses the same mats");
                            targetMeshAsset = lpmi[j].targetMesh;
                            break;
                        }
                    }
                }

                Material[] sourceMaterials = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                TargetMeshTreatment targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                //string newMeshName = sourceMesh.name;
                if (targetMeshAsset != null)
                {
                    // check if this mesh has already been processed
                    processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi);
                    if (lpmi != null)
                    {
                        // check if this mesh uses the same materials as one of the processed meshs
                        bool foundMatch = false;
                        bool targetMeshHasBeenUsed = false;
                        Material[] foundMatchMaterials = null;
                        for (int j = 0; j < lpmi.Count; j++)
                        {
                            if (lpmi[j].targetMesh == targetMeshAsset)
                            {
                                targetMeshHasBeenUsed = true;
                            }
                            if (ComapreMaterials(sourceMaterials, lpmi[j].srcMaterials))
                            {
                                foundMatchMaterials = lpmi[j].targMaterials;
                                foundMatch = true;
                                break;
                            }
                        }

                        if (foundMatch)
                        {
                            // If materials match then we can re-use this processed mesh don't process.
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can re-use this processed mesh don't process. " + targetMeshAsset);
                            targetMeshTreatment = TargetMeshTreatment.reuseMesh;
                            MB_Utility.SetMesh(tr.gameObject, targetMeshAsset);
                            SetMaterials(foundMatchMaterials, targetRenderer);
                            continue;
                        }
                        else
                        {
                            if (targetMeshHasBeenUsed)
                            {
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can't re-use this processed mesh create new with different name. " + targetMeshAsset);
                                //newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), newMeshName);
                                targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                                targetMeshAsset = null;
                            }
                            else
                            {
                                // is it safe to reuse the target mesh
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can replace this processed mesh. " + targetMeshAsset);
                                targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                            }
                        }
                    }
                    else
                    {
                        // source mesh has not been processed can reuse the target mesh
                        targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                    }
                }

                if (targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Replace mesh " + targetMeshAsset);
                    EditorUtility.CopySerialized(sourceMesh, targetMeshAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(targetPrefabName);
                }
                else if (targetMeshTreatment == TargetMeshTreatment.createNewMesh)
                {
                    string newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), sourceMesh.name);
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Create new mesh " + newMeshName);
                    targetMeshAsset = GameObject.Instantiate<Mesh>(sourceMesh);
                    targetMeshAsset.name = newMeshName;
                    MBVersionEditor.PrefabUtility_ApplyPrefabInstance(targPrefabInstance); // Nood to apply changes before saving or we loose them.
                    AssetDatabase.AddObjectToAsset(targetMeshAsset, targetPrefab);
                    MBVersionEditor.PrefabUtility_SavePrefabAsset(targetPrefab);
                    Debug.Assert(targetMeshAsset != null);
                    // need a new mesh
                }

                if (targetMeshTreatment == TargetMeshTreatment.createNewMesh || targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (ProcessMesh(sourceRenderers[i], targetMeshAsset, unityTransforms, mb))
                    {
                        if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Done processing mesh " + targetMeshAsset + " verts " + targetMeshAsset.vertexCount);
                        ProcessedMeshInfo pmi = new ProcessedMeshInfo();
                        pmi.targetMesh = targetMeshAsset;
                        pmi.srcMaterials = sourceMaterials;
                        pmi.targMaterials = mb.meshCombiner.targetRenderer.sharedMaterials;
                        MB_Utility.SetMesh(targetRenderer.gameObject, targetMeshAsset);
                        SetMaterials(pmi.targMaterials, targetRenderer);
                        AddToDictionary(sourceMesh, pmi, processedMeshesSrcToTargetMap);
                    }
                    else
                    {
                        Debug.LogError("Error processing mesh " + targetMeshAsset);
                    }
                }

                MB_Utility.SetMesh(targetRenderer.gameObject, targetMeshAsset);
            }

            MBVersionEditor.PrefabUtility_ApplyPrefabInstance(targPrefabInstance);

            GameObject.DestroyImmediate(srcPrefabInstance);
            GameObject.DestroyImmediate(targPrefabInstance);

            // Destroy obsolete meshes
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(targetPrefabName);
            HashSet<Mesh> usedByTarget = new HashSet<Mesh>();
            foreach (List<ProcessedMeshInfo> ll in processedMeshesSrcToTargetMap.Values)
            {
                for (int i = 0; i < ll.Count; i++)
                {
                    Debug.Assert(ll[i].targetMesh != null, "Have lost targetMesh reference. This can happen when Prefab is replaced.");
                    usedByTarget.Add(ll[i].targetMesh);
                }
            }
            int numDestroyed = 0;
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Mesh)
                {
                    if (!usedByTarget.Contains((Mesh)allAssets[i]) && AssetDatabase.GetAssetPath(allAssets[i]) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        numDestroyed++;
                        GameObject.DestroyImmediate(allAssets[i], true);
                    }
                }
            }

            if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroyed " + numDestroyed + " meshes");
            AssetDatabase.SaveAssets();
            //--------------------------
        }
 


        internal static void _ProcessPrefabRowReplaceTargetPrefab(MB3_BatchPrefabBaker pb, MB3_BatchPrefabBaker.MB3_PrefabBakerRow pr, MB2_TextureBakeResults tbr, List<UnityTransform> unityTransforms, MB3_MeshBaker mb)
        {
            if (pb.LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("==== Processing Source Prefab " + pr.sourcePrefab);

            GameObject srcPrefab = pr.sourcePrefab;
            GameObject targetPrefab = pr.resultPrefab;
            string targetPrefabName = AssetDatabase.GetAssetPath(targetPrefab);
            GameObject srcPrefabInstance = GameObject.Instantiate(srcPrefab);
            Renderer[] rs = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            if (rs.Length < 1)
            {
                Debug.Log("Prefab " + pr.sourcePrefab + " does not have a renderer. Not replacing prefab.");
                GameObject.DestroyImmediate(srcPrefabInstance);
                return;
            }

            GameObject targPrefabInstance = GameObject.Instantiate(targetPrefab);

            // Replace the target prefab asset with the source instance.
            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(targetPrefabName, typeof(GameObject));
            string prefabPath = AssetDatabase.GetAssetPath(obj);
            MBVersionEditor.PrefabUtility_ReplacePrefab(srcPrefabInstance, prefabPath, MB_ReplacePrefabOption.connectToPrefab);

            Renderer[] sourceRenderers = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            Dictionary<Mesh, List<ProcessedMeshInfo>> processedMeshesSrcToTargetMap = new Dictionary<Mesh, List<ProcessedMeshInfo>>();
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (!IsGoodToBake(sourceRenderers[i], tbr))
                {
                    continue;
                }

                Mesh sourceMesh = MB_Utility.GetMesh(sourceRenderers[i].gameObject);

                if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("== Visiting source renderer: " + sourceRenderers[i]);
                // Try to find an existing mesh in the target that we can re-use
                Mesh targetMeshAsset = null;
                Transform tr = FindCorrespondingTransform(srcPrefabInstance.transform, sourceRenderers[i].transform, targPrefabInstance.transform);
                if (tr != null)
                {
                    Mesh targMesh = MB_Utility.GetMesh(tr.gameObject);

                    // Only replace target meshes if they are part of the target prefab.
                    if (AssetDatabase.GetAssetPath(targMesh) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        targetMeshAsset = MB_Utility.GetMesh(tr.gameObject);
                        if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found corresponding transform in target prefab: " + tr + " mesh: " + targetMeshAsset);
                    }
                }

                // Check that we haven't processed this mesh already.
                List<ProcessedMeshInfo> lpmi;
                if (processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi))
                {
                    Material[] srcMats = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                    for (int j = 0; j < lpmi.Count; j++)
                    {
                        if (ComapreMaterials(srcMats, lpmi[j].srcMaterials))
                        {
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found already processed mesh that uses the same mats");
                            targetMeshAsset = lpmi[j].targetMesh;
                            break;
                        }
                    }
                }

                Material[] sourceMaterials = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                TargetMeshTreatment targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                //string newMeshName = sourceMesh.name;
                if (targetMeshAsset != null)
                {
                    // check if this mesh has already been processed
                    processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi);
                    if (lpmi != null)
                    {
                        // check if this mesh uses the same materials as one of the processed meshs
                        bool foundMatch = false;
                        bool targetMeshHasBeenUsed = false;
                        Material[] foundMatchMaterials = null;
                        for (int j = 0; j < lpmi.Count; j++)
                        {
                            if (lpmi[j].targetMesh == targetMeshAsset)
                            {
                                targetMeshHasBeenUsed = true;
                            }
                            if (ComapreMaterials(sourceMaterials, lpmi[j].srcMaterials))
                            {
                                foundMatchMaterials = lpmi[j].targMaterials;
                                foundMatch = true;
                                break;
                            }
                        }

                        if (foundMatch)
                        {
                            // If materials match then we can re-use this processed mesh don't process.
                            if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log(" we can re-use this processed mesh don't process. " + targetMeshAsset);
                            targetMeshTreatment = TargetMeshTreatment.reuseMesh;
                            MB_Utility.SetMesh(sourceRenderers[i].gameObject, targetMeshAsset);
                            SetMaterials(foundMatchMaterials, sourceRenderers[i]);
                            MBVersionEditor.PrefabUtility_ApplyPrefabInstance(srcPrefabInstance);
                            continue;
                        }
                        else
                        {
                            if (targetMeshHasBeenUsed)
                            {
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can't re-use this processed mesh create new with different name. " + targetMeshAsset);
                                //newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), newMeshName);
                                targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                                targetMeshAsset = null;
                            }
                            else
                            {
                                // is it safe to reuse the target mesh
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can replace this processed mesh. " + targetMeshAsset);
                                targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                            }
                        }
                    }
                    else
                    {
                        // source mesh has not been processed can reuse the target mesh
                        targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                    }
                }

                if (targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Replace mesh " + targetMeshAsset);
                    EditorUtility.CopySerialized(sourceMesh, targetMeshAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(targetPrefabName);
                }
                else if (targetMeshTreatment == TargetMeshTreatment.createNewMesh)
                {
                    string newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), sourceMesh.name);
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Create new mesh " + newMeshName);
                    targetMeshAsset = GameObject.Instantiate<Mesh>(sourceMesh);
                    targetMeshAsset.name = newMeshName;
                    AssetDatabase.AddObjectToAsset(targetMeshAsset, targetPrefab);
                    MBVersionEditor.PrefabUtility_SavePrefabAsset(targetPrefab);
                    Debug.Assert(targetMeshAsset != null);
                    // need a new mesh
                }

                if (targetMeshTreatment == TargetMeshTreatment.createNewMesh || targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (ProcessMesh(sourceRenderers[i], targetMeshAsset, unityTransforms, mb))
                    {
                        if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Done processing mesh " + targetMeshAsset + " verts " + targetMeshAsset.vertexCount);
                        ProcessedMeshInfo pmi = new ProcessedMeshInfo();
                        pmi.targetMesh = targetMeshAsset;
                        pmi.srcMaterials = sourceMaterials;
                        pmi.targMaterials = sourceRenderers[i].sharedMaterials;
                        AddToDictionary(sourceMesh, pmi, processedMeshesSrcToTargetMap);
                        SetMaterials(mb.meshCombiner.targetRenderer.sharedMaterials, sourceRenderers[i]);
                        MBVersionEditor.PrefabUtility_ApplyPrefabInstance(srcPrefabInstance); // Need this or "SavePrefabAsset" above doesn't work
                    }
                    else
                    {
                        Debug.LogError("Error processing mesh " + targetMeshAsset);
                    }
                }

                MB_Utility.SetMesh(sourceRenderers[i].gameObject, targetMeshAsset);
                MBVersionEditor.PrefabUtility_ApplyPrefabInstance(srcPrefabInstance); // Need this or "SavePrefabAsset" above doesn't work
            }

            GameObject.DestroyImmediate(srcPrefabInstance);
            GameObject.DestroyImmediate(targPrefabInstance);

            // Destroy obsolete meshes
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(targetPrefabName);
            HashSet<Mesh> usedByTarget = new HashSet<Mesh>();
            foreach (List<ProcessedMeshInfo> ll in processedMeshesSrcToTargetMap.Values)
            {
                for (int i = 0; i < ll.Count; i++)
                {
                    Debug.Assert(ll[i].targetMesh != null, "Have lost targetMesh reference. This can happen when Prefab is replaced.");
                    usedByTarget.Add(ll[i].targetMesh);
                }
            }
            int numDestroyed = 0;
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Mesh)
                {
                    if (!usedByTarget.Contains((Mesh)allAssets[i]) && AssetDatabase.GetAssetPath(allAssets[i]) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        numDestroyed++;
                        GameObject.DestroyImmediate(allAssets[i], true);
                    }
                }
            }

            if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroyed " + numDestroyed + " meshes");

            
            AssetDatabase.SaveAssets();
            //--------------------------
        }

        internal static void _ProcessPrefabRowOnlyMeshesAndMaterials_Legacy_2018_2_AndOlder(MB3_BatchPrefabBaker pb, MB3_BatchPrefabBaker.MB3_PrefabBakerRow pr, MB2_TextureBakeResults tbr, List<UnityTransform> unityTransforms, MB3_MeshBaker mb)
        {
#if UNITY_2018_3_OR_NEWER
            Debug.LogError("Should never call this if newer than 2018.3");
#else
            if (pb.LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("==== Processing Source Prefab " + pr.sourcePrefab);

            GameObject srcPrefab = pr.sourcePrefab;
            GameObject targetPrefab = pr.resultPrefab;
            string targetPrefabName = AssetDatabase.GetAssetPath(targetPrefab);
            GameObject srcPrefabInstance = GameObject.Instantiate(srcPrefab);
            GameObject targPrefabInstance = GameObject.Instantiate(targetPrefab);

            Renderer[] rs = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            if (rs.Length < 1)
            {
                Debug.LogWarning("Prefab " + pr.sourcePrefab + " does not have a renderer");
                GameObject.DestroyImmediate(srcPrefabInstance);
                GameObject.DestroyImmediate(targPrefabInstance);
                return;
            }

            Renderer[] sourceRenderers = srcPrefabInstance.GetComponentsInChildren<Renderer>(true);
            Dictionary<Mesh, List<ProcessedMeshInfo>> processedMeshesSrcToTargetMap = new Dictionary<Mesh, List<ProcessedMeshInfo>>();
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (!IsGoodToBake(sourceRenderers[i], tbr))
                {
                    continue;
                }

                Mesh sourceMesh = MB_Utility.GetMesh(sourceRenderers[i].gameObject);

                if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("== Visiting renderer: " + sourceRenderers[i]);
                // Try to find an existing mesh in the target that we can re-use
                Mesh targetMeshAsset = null;
                Transform tr = FindCorrespondingTransform(srcPrefabInstance.transform, sourceRenderers[i].transform, targPrefabInstance.transform);
                Renderer targetRenderer = null;
                if (tr != null)
                {
                    Mesh targMesh = MB_Utility.GetMesh(tr.gameObject);

                    // Only replace target meshes if they are part of the target prefab.
                    if (AssetDatabase.GetAssetPath(targMesh) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        targetRenderer = tr.GetComponent<Renderer>();
                        if (sourceRenderers[i].GetType() == targetRenderer.GetType())
                        {
                            targetMeshAsset = MB_Utility.GetMesh(tr.gameObject);
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found correspoinding transform in target prefab: " + tr + " mesh: " + targetMeshAsset);
                        }
                        else
                        {
                            Debug.LogError("The renderer on the target prefab matching source prefab " + pr.sourcePrefab + " was not the same kind of renderer " + sourceRenderers[i].name);
                            continue;
                        }

                    }
                }
                else
                {
                    Debug.LogError("There was a renderer in source prefab " + pr.sourcePrefab + " that could not be a matched to a target renderer in the target prefab: " + sourceRenderers[i].name +
                        " This can happen if the hierarchy in the source prefab is different from the hierarchy in the target prefab.");
                    continue;
                }

                // Check that we haven't processed this mesh already.
                List<ProcessedMeshInfo> lpmi;
                if (processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi))
                {
                    Material[] srcMats = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                    for (int j = 0; j < lpmi.Count; j++)
                    {
                        if (ComapreMaterials(srcMats, lpmi[j].srcMaterials))
                        {
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found already processed mesh that uses the same mats");
                            targetMeshAsset = lpmi[j].targetMesh;
                            break;
                        }
                    }
                }

                Material[] sourceMaterials = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                TargetMeshTreatment targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                string newMeshName = sourceMesh.name;
                if (targetMeshAsset != null)
                {
                    // check if this mesh has already been processed
                    processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi);
                    if (lpmi != null)
                    {
                        // check if this mesh uses the same materials as one of the processed meshs
                        bool foundMatch = false;
                        bool targetMeshHasBeenUsed = false;
                        Material[] foundMatchMaterials = null;
                        for (int j = 0; j < lpmi.Count; j++)
                        {
                            if (lpmi[j].targetMesh == targetMeshAsset)
                            {
                                targetMeshHasBeenUsed = true;
                            }
                            if (ComapreMaterials(sourceMaterials, lpmi[j].srcMaterials))
                            {
                                foundMatchMaterials = lpmi[j].targMaterials;
                                foundMatch = true;
                                break;
                            }
                        }

                        if (foundMatch)
                        {
                            // If materials match then we can re-use this processed mesh don't process.
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can re-use this processed mesh don't process. " + targetMeshAsset);
                            targetMeshTreatment = TargetMeshTreatment.reuseMesh;
                            MB_Utility.SetMesh(tr.gameObject, targetMeshAsset);
                            SetMaterials(foundMatchMaterials, targetRenderer);
                            continue;
                        }
                        else
                        {
                            if (targetMeshHasBeenUsed)
                            {
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can't re-use this processed mesh create new with different name. " + targetMeshAsset);
                                newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), newMeshName);
                                targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                                targetMeshAsset = null;
                            }
                            else
                            {
                                // is it safe to reuse the target mesh
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can replace this processed mesh. " + targetMeshAsset);
                                targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                            }
                        }
                    }
                    else
                    {
                        // source mesh has not been processed can reuse the target mesh
                        targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                    }
                }

                if (targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Replace mesh " + targetMeshAsset);
                    EditorUtility.CopySerialized(sourceMesh, targetMeshAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(targetPrefabName);
                }
                else if (targetMeshTreatment == TargetMeshTreatment.createNewMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Create new mesh " + newMeshName);
                    targetMeshAsset = GameObject.Instantiate<Mesh>(sourceMesh);
                    targetMeshAsset.name = newMeshName;
                    AssetDatabase.AddObjectToAsset(targetMeshAsset, targetPrefab);
                    Debug.Assert(targetMeshAsset != null);
                    // need a new mesh
                }

                if (targetMeshTreatment == TargetMeshTreatment.createNewMesh || targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (ProcessMesh(sourceRenderers[i], targetMeshAsset, unityTransforms, mb))
                    {
                        if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Done processing mesh " + targetMeshAsset + " verts " + targetMeshAsset.vertexCount);
                        ProcessedMeshInfo pmi = new ProcessedMeshInfo();
                        pmi.targetMesh = targetMeshAsset;
                        pmi.srcMaterials = sourceMaterials;
                        pmi.targMaterials = sourceRenderers[i].sharedMaterials;
                        AddToDictionary(sourceMesh, pmi, processedMeshesSrcToTargetMap);
                    }
                    else
                    {
                        Debug.LogError("Error processing mesh " + targetMeshAsset);
                    }
                }

                MB_Utility.SetMesh(tr.gameObject, targetMeshAsset);
            }


            // TODO replace this with MBVersionEditor.ReplacePrefab I tried to do this, but when I did,
            // ProcessedMeshInfo.targetMesh becomes null, not sure what is going on there.
            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(targetPrefabName, typeof(GameObject));
            PrefabUtility.ReplacePrefab(targPrefabInstance, obj, ReplacePrefabOptions.ReplaceNameBased);

            GameObject.DestroyImmediate(srcPrefabInstance);
            GameObject.DestroyImmediate(targPrefabInstance);

            // Destroy obsolete meshes
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(targetPrefabName);
            HashSet<Mesh> usedByTarget = new HashSet<Mesh>();
            foreach (List<ProcessedMeshInfo> ll in processedMeshesSrcToTargetMap.Values)
            {
                for (int i = 0; i < ll.Count; i++)
                {
                    usedByTarget.Add(ll[i].targetMesh);
                }
            }
            int numDestroyed = 0;
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Mesh)
                {
                    if (!usedByTarget.Contains((Mesh)allAssets[i]) && AssetDatabase.GetAssetPath(allAssets[i]) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        numDestroyed++;
                        GameObject.DestroyImmediate(allAssets[i], true);
                    }
                }
            }

            if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroyed " + numDestroyed + " meshes");
            AssetDatabase.SaveAssets();
            //--------------------------
#endif
        }

        internal static void _ProcessPrefabRowReplaceTargetPrefab_Legacy_2018_2_AndOlder(MB3_BatchPrefabBaker pb, MB3_BatchPrefabBaker.MB3_PrefabBakerRow pr, MB2_TextureBakeResults tbr, List<UnityTransform> unityTransforms, MB3_MeshBaker mb)
        {
#if UNITY_2018_3_OR_NEWER
            Debug.LogError("Should never call this if newer than 2018.3");
#else
            if (pb.LOG_LEVEL >= MB2_LogLevel.info) Debug.Log("==== Processing Source Prefab " + pr.sourcePrefab);

            GameObject srcPrefab = pr.sourcePrefab;
            GameObject targetPrefab = pr.resultPrefab;
            string targetPrefabName = AssetDatabase.GetAssetPath(targetPrefab);
            GameObject prefabInstance = GameObject.Instantiate(srcPrefab);

            Renderer[] rs = prefabInstance.GetComponentsInChildren<Renderer>(true);
            if (rs.Length < 1)
            {
                Debug.Log("Prefab " + pr.sourcePrefab + " does not have a renderer. Not replacing prefab.");
                GameObject.DestroyImmediate(prefabInstance);
                return;
            }

            Renderer[] sourceRenderers = prefabInstance.GetComponentsInChildren<Renderer>(true);

            Dictionary<Mesh, List<ProcessedMeshInfo>> processedMeshesSrcToTargetMap = new Dictionary<Mesh, List<ProcessedMeshInfo>>();
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (!IsGoodToBake(sourceRenderers[i], tbr))
                {
                    continue;
                }

                Mesh sourceMesh = MB_Utility.GetMesh(sourceRenderers[i].gameObject);

                if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("== Visiting source renderer: " + sourceRenderers[i]);
                // Try to find an existing mesh in the target that we can re-use
                Mesh targetMeshAsset = null;
                Transform tr = FindCorrespondingTransform(prefabInstance.transform, sourceRenderers[i].transform, targetPrefab.transform);
                if (tr != null)
                {
                    Mesh targMesh = MB_Utility.GetMesh(tr.gameObject);

                    // Only replace target meshes if they are part of the target prefab.
                    if (AssetDatabase.GetAssetPath(targMesh) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        targetMeshAsset = MB_Utility.GetMesh(tr.gameObject);
                        if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found corresponding transform in target prefab: " + tr + " mesh: " + targetMeshAsset);
                    }
                }

                // Check that we haven't processed this mesh already.
                List<ProcessedMeshInfo> lpmi;
                if (processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi))
                {
                    Material[] srcMats = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                    for (int j = 0; j < lpmi.Count; j++)
                    {
                        if (ComapreMaterials(srcMats, lpmi[j].srcMaterials))
                        {
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log("Found already processed mesh that uses the same mats");
                            targetMeshAsset = lpmi[j].targetMesh;
                            break;
                        }
                    }
                }

                Material[] sourceMaterials = MB_Utility.GetGOMaterials(sourceRenderers[i].gameObject);
                TargetMeshTreatment targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                string newMeshName = sourceMesh.name;
                if (targetMeshAsset != null)
                {
                    // check if this mesh has already been processed
                    processedMeshesSrcToTargetMap.TryGetValue(sourceMesh, out lpmi);
                    if (lpmi != null)
                    {
                        // check if this mesh uses the same materials as one of the processed meshs
                        bool foundMatch = false;
                        bool targetMeshHasBeenUsed = false;
                        Material[] foundMatchMaterials = null;
                        for (int j = 0; j < lpmi.Count; j++)
                        {
                            if (lpmi[j].targetMesh == targetMeshAsset)
                            {
                                targetMeshHasBeenUsed = true;
                            }
                            if (ComapreMaterials(sourceMaterials, lpmi[j].srcMaterials))
                            {
                                foundMatchMaterials = lpmi[j].targMaterials;
                                foundMatch = true;
                                break;
                            }
                        }

                        if (foundMatch)
                        {
                            // If materials match then we can re-use this processed mesh don't process.
                            if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can re-use this processed mesh don't process. " + targetMeshAsset);
                            targetMeshTreatment = TargetMeshTreatment.reuseMesh;
                            MB_Utility.SetMesh(sourceRenderers[i].gameObject, targetMeshAsset);
                            SetMaterials(foundMatchMaterials, sourceRenderers[i]);
                            continue;
                        }
                        else
                        {
                            if (targetMeshHasBeenUsed)
                            {
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can't re-use this processed mesh create new with different name. " + targetMeshAsset);
                                newMeshName = GetNameForNewMesh(AssetDatabase.GetAssetPath(targetPrefab), newMeshName);
                                targetMeshTreatment = TargetMeshTreatment.createNewMesh;
                                targetMeshAsset = null;
                            }
                            else
                            {
                                // is it safe to reuse the target mesh
                                // we need a new target mesh with a safe different name
                                if (pb.LOG_LEVEL >= MB2_LogLevel.trace) Debug.Log(" we can replace this processed mesh. " + targetMeshAsset);
                                targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                            }
                        }
                    }
                    else
                    {
                        // source mesh has not been processed can reuse the target mesh
                        targetMeshTreatment = TargetMeshTreatment.replaceMesh;
                    }
                }

                if (targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Replace mesh " + targetMeshAsset);
                    EditorUtility.CopySerialized(sourceMesh, targetMeshAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(targetPrefabName);
                }
                else if (targetMeshTreatment == TargetMeshTreatment.createNewMesh)
                {
                    if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Create new mesh " + newMeshName);
                    targetMeshAsset = GameObject.Instantiate<Mesh>(sourceMesh);
                    targetMeshAsset.name = newMeshName;
                    AssetDatabase.AddObjectToAsset(targetMeshAsset, targetPrefab);
                    Debug.Assert(targetMeshAsset != null);
                    // need a new mesh
                }

                if (targetMeshTreatment == TargetMeshTreatment.createNewMesh || targetMeshTreatment == TargetMeshTreatment.replaceMesh)
                {
                    if (ProcessMesh(sourceRenderers[i], targetMeshAsset, unityTransforms, mb))
                    {
                        if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Done processing mesh " + targetMeshAsset + " verts " + targetMeshAsset.vertexCount);
                        ProcessedMeshInfo pmi = new ProcessedMeshInfo();
                        pmi.targetMesh = targetMeshAsset;
                        pmi.srcMaterials = sourceMaterials;
                        pmi.targMaterials = sourceRenderers[i].sharedMaterials;
                        AddToDictionary(sourceMesh, pmi, processedMeshesSrcToTargetMap);
                    }
                    else
                    {
                        Debug.LogError("Error processing mesh " + targetMeshAsset);
                    }
                }

                MB_Utility.SetMesh(sourceRenderers[i].gameObject, targetMeshAsset);
            }

            // TODO replace this with MBVersionEditor.ReplacePrefab I tried to do this, but when I did,
            // ProcessedMeshInfo.targetMesh becomes null, not sure what is going on there.
            GameObject obj = (GameObject)AssetDatabase.LoadAssetAtPath(targetPrefabName, typeof(GameObject));
            PrefabUtility.ReplacePrefab(prefabInstance, obj, ReplacePrefabOptions.ReplaceNameBased);
            GameObject.DestroyImmediate(prefabInstance);

            // Destroy obsolete meshes
            UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(targetPrefabName);
            HashSet<Mesh> usedByTarget = new HashSet<Mesh>();
            foreach (List<ProcessedMeshInfo> ll in processedMeshesSrcToTargetMap.Values)
            {
                for (int i = 0; i < ll.Count; i++)
                {
                    usedByTarget.Add(ll[i].targetMesh);
                }
            }
            int numDestroyed = 0;
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Mesh)
                {
                    if (!usedByTarget.Contains((Mesh)allAssets[i]) && AssetDatabase.GetAssetPath(allAssets[i]) == AssetDatabase.GetAssetPath(targetPrefab))
                    {
                        numDestroyed++;
                        GameObject.DestroyImmediate(allAssets[i], true);
                    }
                }
            }

            if (pb.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Destroyed " + numDestroyed + " meshes");
            AssetDatabase.SaveAssets();
            //--------------------------
#endif
        }


        private static string GetNameForNewMesh(string prefabPath, string baseName)
        {
            // get all Mesh assets in prefab
            UnityEngine.Object[] objs = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
            string[] oldNames = new string[objs.Length]; // TODO get these
            for (int i = 0; i < oldNames.Length; i++)
            {
                oldNames[i] = objs[i].name;
            }

            bool isUnique = false;
            int idx = 0;
            string name = baseName;
            while (!isUnique)
            {
                bool wasAMatch = false;
                for (int i = 0; i < oldNames.Length; i++)
                {
                    if (oldNames[i].Equals(name))
                    {
                        wasAMatch = true;
                        break;
                    }
                }
                if (wasAMatch)
                {
                    idx++;
                    name = baseName + idx;
                }
                else
                {
                    isUnique = true;
                }
            }
            return name;
        }

        private static bool ComapreMaterials(Material[] a, Material[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static void AddToDictionary(Mesh sourceMesh, ProcessedMeshInfo pmi, Dictionary<Mesh, List<ProcessedMeshInfo>> dict)
        {
            List<ProcessedMeshInfo> lpmi;
            if (!dict.ContainsKey(sourceMesh))
            {
                lpmi = new List<ProcessedMeshInfo>();
                dict[sourceMesh] = lpmi;
            }
            else
            {
                lpmi = dict[sourceMesh];
            }
            lpmi.Add(pmi);
        }

        private static bool ProcessMesh(Renderer r, Mesh m, List<UnityTransform> unityTransforms, MB3_MeshBaker mb)
        {
            unityTransforms.Clear();
            // position rotation and scale are baked into combined mesh.
            // Remember all the transforms settings then
            // record transform values to root of hierarchy
            Transform t = r.transform;
            if (t != t.root)
            {
                do
                {
                    unityTransforms.Add(new UnityTransform(t));
                    t = t.parent;
                } while (t != null && t != t.root);
            }

            //add the root
            unityTransforms.Add(new UnityTransform(t.root));

            //position at identity
            for (int k = 0; k < unityTransforms.Count; k++)
            {
                unityTransforms[k].t.localPosition = Vector3.zero;
                unityTransforms[k].t.localRotation = Quaternion.identity;
                unityTransforms[k].t.localScale = Vector3.one;
            }

            //bake the mesh
            MB3_MeshCombinerSingle mc = (MB3_MeshCombinerSingle)mb.meshCombiner;
            if (!MB3_BakeInPlace.BakeOneMesh(mc, m, r.gameObject))
            {
                return false;
            }

            //replace the mesh
            if (r is MeshRenderer)
            {
                MeshFilter mf = r.gameObject.GetComponent<MeshFilter>();
                mf.sharedMesh = m;
            }
            else
            { //skinned mesh
                SkinnedMeshRenderer smr = r.gameObject.GetComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = m;
                smr.bones = ((SkinnedMeshRenderer)mc.targetRenderer).bones;
            }

            if (mc.targetRenderer != null)
            {
                SetMaterials(mc.targetRenderer.sharedMaterials, r);
            }

            //restore the transforms
            for (int k = 0; k < unityTransforms.Count; k++)
            {
                unityTransforms[k].t.localPosition = unityTransforms[k].p;
                unityTransforms[k].t.localRotation = unityTransforms[k].q;
                unityTransforms[k].t.localScale = unityTransforms[k].s;
            }

            mc.SetMesh(null);
            return true;
        }

        private static void SetMaterials(Material[] sharedMaterials, Renderer r)
        {
            //First try to get the materials from the target renderer. This is because the mesh may have fewer submeshes than number of result materials if some of the submeshes had zero length tris.
            //If we have just baked then materials on the target renderer will be correct wheras materials on the textureBakeResult may not be correct.

            Material[] sharedMats = new Material[sharedMaterials.Length];
            for (int i = 0; i < sharedMats.Length; i++)
            {
                sharedMats[i] = sharedMaterials[i];
            }
            if (r is SkinnedMeshRenderer)
            {
                r.sharedMaterial = null;
                r.sharedMaterials = sharedMats;
            }
            else
            {
                r.sharedMaterial = null;
                r.sharedMaterials = sharedMats;
            }
        }

        private static bool IsGoodToBake(Renderer r, MB2_TextureBakeResults tbr)
        {
            if (r == null) return false;
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
            {
                return false;
            }
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!tbr.ContainsMaterial(mats[i]))
                {
                    Debug.LogWarning("Mesh on " + r + " uses a material " + mats[i] + " that is not in the list of materials. This mesh will not be baked. The original mesh and material will be used in the result prefab.");
                    return false;
                }
            }
            if (MB_Utility.GetMesh(r.gameObject) == null)
            {
                return false;
            }
            return true;
        }

        internal static Transform FindCorrespondingTransform(Transform srcRoot, Transform srcChild,
                                             Transform targRoot)
        {
            if (srcRoot == srcChild) return targRoot;

            //		Debug.Log ("start ============");
            //build the path to the root in the source prefab
            List<Transform> path_root2child = new List<Transform>();
            Transform t = srcChild;
            do
            {
                path_root2child.Insert(0, t);
                t = t.parent;
            } while (t != null && t != t.root && t != srcRoot);
            if (t == null)
            {
                Debug.LogError("scrChild was not child of srcRoot " + srcRoot + " " + srcChild);
                return null;
            }
            path_root2child.Insert(0, srcRoot);
            //		Debug.Log ("path to root for " + srcChild + " " + path_root2child.Count);

            //try to find a matching path in the target prefab
            t = targRoot;
            for (int i = 1; i < path_root2child.Count; i++)
            {
                Transform tSrc = path_root2child[i - 1];
                //try to find child in same position with same name
                int srcIdx = TIndexOf(tSrc, path_root2child[i]);
                if (srcIdx < t.childCount && path_root2child[i].name.Equals(t.GetChild(srcIdx).name))
                {
                    t = t.GetChild(srcIdx);
                    //				Debug.Log ("found child in same position with same name " + t);
                    continue;
                }
                //try to find child with same name
                for (int j = 0; j < t.childCount; j++)
                {
                    if (t.GetChild(j).name.Equals(path_root2child[i].name))
                    {
                        t = t.GetChild(j);
                        //					Debug.Log ("found child with same name " + t);
                        continue;
                    }
                }
                t = null;
                break;
            }
            //		Debug.Log ("end =============== " + t);
            return t;
        }

        private static int TIndexOf(Transform p, Transform c)
        {
            for (int i = 0; i < p.childCount; i++)
            {
                if (c == p.GetChild(i))
                {
                    return i;
                }
            }
            return -1;
        }

        public static string ConvertProjectRelativePathToFullPath(string projectRelativePath)
        {
            if (projectRelativePath.StartsWith("Assets"))
            {
                string fullPath = Application.dataPath + projectRelativePath.Substring(6);
                return fullPath;
            } else
            {
                return projectRelativePath;
            }
        }

        // This is a more robust way to convert to relative paths, but can only convert paths on the same machine
        public static string ConvertFullPathToProjectRelativePath(string path)
        {
            if (path != null && path.Length > 0)
            {
                Uri projectFolder = new Uri(Application.dataPath);
                Uri outputFolder = new Uri(path);
                Uri relativeUri = projectFolder.MakeRelativeUri(outputFolder);
                string relativePath = MBVersion.UnescapeURL(relativeUri.ToString());
                if (relativePath.Length == 0)
                {
                    relativePath = "Assets/";
                }

                if (!relativePath.StartsWith("Assets/"))
                {
                    Debug.LogError("Bad folder path. The folder must be in the project Assets folder.");
                    return "";
                } else
                {
                    return relativePath;
                }
            } else
            {
                return path;
            }
        }

        // Will convert any path to relative, regardless of if it came from this machine or one with a different root
        public static string ConvertAnyPathToProjectRelativePath(string path)
        {
            if(path.Length > 0){
                if(Regex.Matches(path, "Assets").Count == 1 && path.EndsWith("/Assets"))
                {
                    path = path + "/";
                }
                // Potential Bug: User could have their unity project nested in a folder named *Assets (eg. C:/GameAssets/<project-name>/Assets)
                // But if we check for this using Application.dataPath it will no longer handle paths with a different root
                int relativeIdx = path.IndexOf("Assets/");
                if(relativeIdx > -1){
                    return path.Substring(relativeIdx);
                }
                else{
                    Debug.LogError("Bad folder path. The folder must be in the project Assets folder.");
                    if(Regex.Matches(path, "Assets").Count > 1)
                    {
                        Debug.LogError("Multiple folders with 'Assets' in their name found in the path may be causing this error");
                    }
                    return null;
                }
            }
            return null;
        }

        public static bool ValidateFolderIsInProject(string fieldName, string folder)
        {
            if (folder == null)
            {
                Debug.LogError(fieldName + " must be set");
                return false;
            }

            string relativePath = MB_BatchPrefabBakerEditorFunctions.ConvertAnyPathToProjectRelativePath(folder);
            string gid = AssetDatabase.AssetPathToGUID(relativePath);
            if (gid == null || gid.Length == 0)
            {
                Debug.LogError(fieldName + " must be an existing folder in the Unity project Assets folder");
                return false;
            }

            return true;
        }

        public static void CreateEmptyOutputPrefabs(string outputFolder, MB3_BatchPrefabBaker target)
        {
            if (!ValidateFolderIsInProject("Output Folder", outputFolder))
            {
                return; 
            }

            if (outputFolder.StartsWith("Assets")) outputFolder = ConvertProjectRelativePathToFullPath(outputFolder);
            int numCreated = 0;
            int numSkippedSrcNull = 0;
            int numSkippedAlreadyExisted = 0;
            MB3_BatchPrefabBaker prefabBaker = (MB3_BatchPrefabBaker)target;
            for (int i = 0; i < prefabBaker.prefabRows.Length; i++)
            {
                if (prefabBaker.prefabRows[i].sourcePrefab != null)
                {
                    if (prefabBaker.prefabRows[i].resultPrefab == null)
                    {
                        string outName = outputFolder + "/" + prefabBaker.prefabRows[i].sourcePrefab.name + ".prefab";
                        outName = outName.Replace(Application.dataPath, "");
                        outName = "Assets" + outName;
                        outName = AssetDatabase.GenerateUniqueAssetPath(outName);
                        GameObject go = new GameObject(prefabBaker.prefabRows[i].sourcePrefab.name);
                        prefabBaker.prefabRows[i].resultPrefab = MBVersionEditor.PrefabUtility_CreatePrefab(outName, go);
                        GameObject.DestroyImmediate(go);
                        numCreated++;
                    }
                    else
                    {
                        numSkippedAlreadyExisted++;
                    }
                }
                else
                {
                    numSkippedSrcNull++;
                }
            }
            Debug.Log(String.Format("Created {0} prefabs. Skipped {1} because source prefab was null. Skipped {2} because the result prefab was already assigned", numCreated, numSkippedSrcNull, numSkippedAlreadyExisted));
        }
    }
}
