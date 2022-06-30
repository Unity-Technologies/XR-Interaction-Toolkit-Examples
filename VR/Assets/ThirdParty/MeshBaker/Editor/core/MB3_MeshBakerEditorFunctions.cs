using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using DigitalOpus.MB.Core;
using UnityEditor;
using DigitalOpus.MB.MBEditor;

public class MB3_MeshBakerEditorFunctions
{

    /// <summary>
    /// Used by UnityEditorInspectors for background colors
    /// </summary>
    public static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }

    public static bool BakeIntoCombined(MB3_MeshBakerCommon mom, out bool createdDummyTextureBakeResults)
    {
        SerializedObject so = null;
        return BakeIntoCombined(mom, out createdDummyTextureBakeResults, ref so);
    }

    /// <summary>
    ///  Bakes a combined mesh.
    /// </summary>
    /// <param name="so">If this is being called from Inspector code then pass in the SerializedObject for the component.
    /// This is necessary for "bake into prefab" which can corrupt the SerializedObject.</param>
    public static bool BakeIntoCombined(MB3_MeshBakerCommon mom, out bool createdDummyTextureBakeResults, ref SerializedObject so)
    {
        MB2_OutputOptions prefabOrSceneObject = mom.meshCombiner.outputOption;
        createdDummyTextureBakeResults = false;

        // Initial Validate
        {
            if (mom.meshCombiner.resultSceneObject != null &&
                (MBVersionEditor.PrefabUtility_GetPrefabType(mom.meshCombiner.resultSceneObject) == MB_PrefabType.modelPrefabAsset ||
                 MBVersionEditor.PrefabUtility_GetPrefabType(mom.meshCombiner.resultSceneObject) == MB_PrefabType.prefabAsset))
            {
                Debug.LogWarning("Result Game Object was a project asset not a scene object instance. Clearing this field.");
                mom.meshCombiner.resultSceneObject = null;
            }

            if (prefabOrSceneObject != MB2_OutputOptions.bakeIntoPrefab && prefabOrSceneObject != MB2_OutputOptions.bakeIntoSceneObject)
            {
                Debug.LogError("Paramater prefabOrSceneObject must be bakeIntoPrefab or bakeIntoSceneObject");
                return false;
            }

            if (prefabOrSceneObject == MB2_OutputOptions.bakeIntoPrefab)
            {
                if (MB3_MeshCombiner.EVAL_VERSION)
                {
                    Debug.LogError("Cannot BakeIntoPrefab with evaluation version.");
                    return false;
                }

                if (mom.resultPrefab == null)
                {
                    Debug.LogError("Need to set the Combined Mesh Prefab field. Create a prefab asset, drag an empty game object into it, and drag it to the 'Combined Mesh Prefab' field.");
                    return false;
                }

                string prefabPth = AssetDatabase.GetAssetPath(mom.resultPrefab);
                if (prefabPth == null || prefabPth.Length == 0)
                {
                    Debug.LogError("Could not save result to prefab. Result Prefab value is not a project asset. Is it an instance in the scene?");
                    return false;
                }
            }
        }

        {
            // Find or create texture bake results
            MB3_TextureBaker tb = mom.GetComponentInParent<MB3_TextureBaker>();
            if (mom.textureBakeResults == null && tb != null)
            {
                mom.textureBakeResults = tb.textureBakeResults;
            }

            if (mom.textureBakeResults == null)
            {
                if (_OkToCreateDummyTextureBakeResult(mom))
                {
                    createdDummyTextureBakeResults = true;
                    List<GameObject> gos = mom.GetObjectsToCombine();
                    if (mom.GetNumObjectsInCombined() > 0)
                    {
                        if (mom.clearBuffersAfterBake) { mom.ClearMesh(); }
                        else
                        {
                            Debug.LogError("'Texture Bake Result' must be set to add more objects to a combined mesh that already contains objects. Try enabling 'clear buffers after bake'");
                            return false;
                        }
                    }
                    mom.textureBakeResults = MB2_TextureBakeResults.CreateForMaterialsOnRenderer(gos.ToArray(), mom.meshCombiner.GetMaterialsOnTargetRenderer());
                    if (mom.meshCombiner.LOG_LEVEL >= MB2_LogLevel.debug) { Debug.Log("'Texture Bake Result' was not set. Creating a temporary one. Each material will be mapped to a separate submesh."); }
                }
            }
        }

        // Second level of validation now that TextureBakeResults exists.
        MB2_ValidationLevel vl = Application.isPlaying ? MB2_ValidationLevel.quick : MB2_ValidationLevel.robust;
        if (!MB3_MeshBakerRoot.DoCombinedValidate(mom, MB_ObjsToCombineTypes.sceneObjOnly, new MB3_EditorMethods(), vl))
        {
            return false;
        }

        // Add Delete Game Objects
        bool success;
        if (prefabOrSceneObject == MB2_OutputOptions.bakeIntoSceneObject)
        {
            success = _BakeIntoCombinedSceneObject(mom, createdDummyTextureBakeResults, ref so);
        }
        else if (prefabOrSceneObject == MB2_OutputOptions.bakeIntoPrefab)
        {
            success = _BakeIntoCombinedPrefab(mom, createdDummyTextureBakeResults, ref so);
        } else
        {
            Debug.LogError("Should be impossible.");
            success = false;
        }

        if (mom.clearBuffersAfterBake) { mom.meshCombiner.ClearBuffers(); }
        if (createdDummyTextureBakeResults) MB_Utility.Destroy(mom.textureBakeResults);
        return success;
    }

    private static bool _BakeIntoCombinedSceneObject(MB3_MeshBakerCommon mom, bool createdDummyTextureBakeResults, ref SerializedObject so)
    {
        bool success;
        mom.ClearMesh();
        if (mom.AddDeleteGameObjects(mom.GetObjectsToCombine().ToArray(), null, false))
        {
            success = true;
            mom.Apply(UnwrapUV2);

            if (createdDummyTextureBakeResults)
            {
                Debug.Log(String.Format("Successfully baked {0} meshes each material is mapped to its own submesh.", mom.GetObjectsToCombine().Count));
            }
            else
            {
                Debug.Log(String.Format("Successfully baked {0} meshes", mom.GetObjectsToCombine().Count));
            }
        }
        else
        {
            success = false;
        }

        return success;
    }

    private static bool _BakeIntoCombinedPrefab(MB3_MeshBakerCommon mom, bool createdDummyTextureBakeResults, ref SerializedObject so)
    {
        bool success = false;

        List<Transform> tempPrefabInstanceRoots = null;
        GameObject[] objsToCombine = mom.GetObjectsToCombine().ToArray();
        if (mom.meshCombiner.settings.renderType == MB_RenderType.skinnedMeshRenderer)
        {
            tempPrefabInstanceRoots = new List<Transform>();
            // We are going to move bones of source objs and transforms into our combined mesh prefab so make some duplicates
            // so that we don't destroy a setup.
            _DuplicateSrcObjectInstancesAndUnpack(mom.meshCombiner.settings.renderType, objsToCombine, tempPrefabInstanceRoots);
        }
        try
        {
            MB3_EditorMethods editorMethods = new MB3_EditorMethods();
            mom.ClearMesh(editorMethods);
            if (mom.AddDeleteGameObjects(objsToCombine, null, false))
            {
                success = true;
                mom.Apply(UnwrapUV2);


                if (createdDummyTextureBakeResults)
                {
                    Debug.Log(String.Format("Successfully baked {0} meshes each material is mapped to its own submesh.", mom.GetObjectsToCombine().Count));
                }
                else
                {
                    Debug.Log(String.Format("Successfully baked {0} meshes", mom.GetObjectsToCombine().Count));
                }

                string prefabPth = AssetDatabase.GetAssetPath(mom.resultPrefab);
                if (prefabPth == null || prefabPth.Length == 0)
                {
                    Debug.LogError("Could not save result to prefab. Result Prefab value is not an Asset.");
                    success = false;
                }
                else
                {
                    string baseName = Path.GetFileNameWithoutExtension(prefabPth);
                    string folderPath = prefabPth.Substring(0, prefabPth.Length - baseName.Length - 7);
                    string newFilename = folderPath + baseName + "-mesh";
                    SaveMeshsToAssetDatabase(mom, folderPath, newFilename);
                    RebuildPrefab(mom, ref so, mom.resultPrefabLeaveInstanceInSceneAfterBake, tempPrefabInstanceRoots, objsToCombine);
                }
            }
            else
            {
                success = false;
            }

        }
        catch
        {
            throw;
        } finally
        {
            // Clean up temporary created instances. If success was true then they should have been added to a prefab
            // and cleaned up for us.
            if (success == false)
            {
                if (tempPrefabInstanceRoots != null)
                {
                    for (int i = 0; i < tempPrefabInstanceRoots.Count; i++)
                    {
                        MB_Utility.Destroy(tempPrefabInstanceRoots[i]);
                    }
                }
            }
        }

        return success;
    }

    /// <summary>
    /// We will modify the source objects (unpack prefabs and re-organize prefabs) so duplicate them.
    /// </summary>
    /// <param name="tempGameObjectInstances"></param>
    private static void _MoveBonesToCombinedMeshPrefabAndDeleteRenderers(Transform newPrefabInstanceRoot, List<Transform> tempGameObjectInstances, GameObject[] srcRenderers)
    {
        for (int i = 0; i < srcRenderers.Length; i++)
        {
            MeshRenderer mr = srcRenderers[i].GetComponent<MeshRenderer>();
            if (mr != null) MB_Utility.Destroy(mr);
            MeshFilter mf = srcRenderers[i].GetComponent<MeshFilter>();
            if (mf != null) MB_Utility.Destroy(mf);
            SkinnedMeshRenderer smr = srcRenderers[i].GetComponent<SkinnedMeshRenderer>();
            if (smr != null) MB_Utility.Destroy(smr);
        }

        for (int i = 0; i < tempGameObjectInstances.Count; i++)
        {
            Transform tt = tempGameObjectInstances[i];
            tempGameObjectInstances[i].parent = newPrefabInstanceRoot;
        }
    }

    /// <summary>
    /// We will modify the source object so duplicate them
    /// </summary>
    /// <param name="tempGameObjectInstances"></param>
    public static void _DuplicateSrcObjectInstancesAndUnpack(MB_RenderType renderType, GameObject[] objsToCombine, List<Transform> tempGameObjectInstances)
    {
        Debug.Assert(renderType == MB_RenderType.skinnedMeshRenderer, "RenderType must be Skinned Mesh Renderer");
        // first pass, collect the prefab-instance roots for each of the src objects.
        Transform[] sceneInstanceParents = new Transform[objsToCombine.Length];
        for (int i = 0; i < objsToCombine.Length; i++)
        {
            // Get the prefab root
            GameObject pr = null;
            {
                MB_PrefabType pt = MBVersionEditor.PrefabUtility_GetPrefabType(objsToCombine[i]);
                if (pt == MB_PrefabType.scenePefabInstance || pt == MB_PrefabType.isInstanceAndNotAPartOfAnyPrefab)
                {
                    pr = MBVersionEditor.PrefabUtility_GetPrefabInstanceRoot(objsToCombine[i]);
                }

                if (pr == null)
                {
                    pr = _FindCommonAncestorForBonesAnimatorAndSmr(objsToCombine[i]);
                    
                }
            }

            sceneInstanceParents[i] = pr.transform;
        }

        // second pass, some of the parents could be children of other parents. ensure that we are
        // using the uppermost ancestor for all.
        for (int i = 0; i < objsToCombine.Length; i++)
        {
            sceneInstanceParents[i] = _FindUppermostParent(objsToCombine[i], sceneInstanceParents);
        }

        // Now build a map of sceneInstanceParents to the renderers contained beneath.
        Dictionary<Transform, List<Transform>> srcPrefabInstances2Renderers = new Dictionary<Transform, List<Transform>>();
        for (int i = 0; i < objsToCombine.Length; i++)
        {
            List<Transform> renderersUsed;
            if (!srcPrefabInstances2Renderers.TryGetValue(sceneInstanceParents[i], out renderersUsed))
            {
                renderersUsed = new List<Transform>();
                srcPrefabInstances2Renderers.Add(sceneInstanceParents[i], renderersUsed);
            }

            renderersUsed.Add(objsToCombine[i].transform);
        }

        // Duplicate the prefab-instance-root scene objects
        List<Transform> srcRoots = new List<Transform>(srcPrefabInstances2Renderers.Keys);
        List<Transform> targRoots = new List<Transform>();
        for (int i = 0; i < srcRoots.Count; i++)
        {
            Transform src = srcRoots[i];
            GameObject n = GameObject.Instantiate<GameObject>(src.gameObject);
            n.transform.rotation = src.rotation;
            n.transform.position = src.position;
            n.transform.localScale = src.localScale;
            targRoots.Add(n.transform);
            tempGameObjectInstances.Add(targRoots[i]);
            _CheckSrcRootScale(renderType, src);
        }

        // Find the correct duplicated objsToCombine in the new instances that maps to objs in "objsToCombine".
        List<GameObject> newObjsToCombine = new List<GameObject>();
        for (int i = 0; i < srcRoots.Count; i++)
        {
            List<Transform> renderers = srcPrefabInstances2Renderers[srcRoots[i]];
            for (int j = 0; j < renderers.Count; j++)
            {
                Transform t = MB_BatchPrefabBakerEditorFunctions.FindCorrespondingTransform(srcRoots[i], renderers[j], targRoots[i]);
                Debug.Assert(!newObjsToCombine.Contains(t.gameObject));
                newObjsToCombine.Add(t.gameObject);
            }

        }
        Debug.Assert(newObjsToCombine.Count == objsToCombine.Length);

        for (int i = 0; i < newObjsToCombine.Count; i++)
        {
            //GameObject go = newObjsToCombine[i];
            //SerializedObject so = null;
            //MB_PrefabType pt = MBVersionEditor.GetPrefabType(go);
            //if (pt == MB_PrefabType.sceneInstance)
            //{
            //    MBVersionEditor.UnpackPrefabInstance(go, ref so);
            //}

            objsToCombine[i] = newObjsToCombine[i];
        }
    }

    private static GameObject _FindCommonAncestorForBonesAnimatorAndSmr(GameObject sceneInstance)
    {
        Renderer mr = MB_Utility.GetRenderer(sceneInstance);
        Debug.Assert(mr != null, "Should only be called on a GameObject with a Renderer");

        Transform lca = sceneInstance.transform;

        if (mr is SkinnedMeshRenderer)
        {
            // find lowest common ancestor of bones and SMR
            Transform[] bones = ((SkinnedMeshRenderer)mr).bones;
            HashSet<Transform> ancestorsOfLCA = new HashSet<Transform>();

            _CollectAllAncestors(ancestorsOfLCA, lca);

            // visit each other bone, find LCA of LCA and bone
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    lca = _FindLowestCommonAncestor(ancestorsOfLCA, lca, bones[i], sceneInstance);
                }
            }
        }

        // Search ancestors for an Animator/Animation
        {
            Transform t = lca;
            while (t != null)
            {
                if (t.GetComponent<Animator>() != null ||
                    t.GetComponent<Animation>() != null)
                {
                    //Debug.Log("Found ancestor with Animation/Animator: " + t);
                    lca = t;
                    break;
                }

                t = t.parent;
            }

            return lca.gameObject;
        }
    }

    private static Transform _FindLowestCommonAncestor(HashSet<Transform> ancestorsOfLCA, Transform lca, Transform b, GameObject db_Renderer)
    {
        // visit all ancestors of b
        Transform newLca = lca;
        Transform t = b;
        bool found = false;
        while (t != null)
        {
            if (ancestorsOfLCA.Contains(t))
            {
                found = true;
                newLca = t;
                break;
            }

            t = t.parent;
        }

        if (found)
        {
            //Debug.Log("Finding all ancestors for: " + lca + ", " + b + "  found: " + newLca);
            if (newLca != lca)
            {
                _CollectAllAncestors(ancestorsOfLCA, newLca);
            }

            return newLca;
        }
        else
        {
            Debug.LogError("Renderer '" + db_Renderer + "' does not share a common ancestor in the hierarcy with its bones. If you are baking a prefab, then the prefab will not contain the bones. Try creating a GameObject parent for '" + db_Renderer + "' and its bones and re-baking.");
            return null;
        }
    }

    private static void _CollectAllAncestors(HashSet<Transform> ancestorsOfA, Transform targ)
    {
        Transform t = targ;
        ancestorsOfA.Clear();
        ancestorsOfA.Add(t);
        while (t != null)
        {
            t = t.parent;
            ancestorsOfA.Add(t);
        }

        //Debug.Log("_CollectAllAncestors of: " + targ + " found: " + ancestorsOfA.Count);
    }

    private static void _CheckSrcRootScale(MB_RenderType renderType, Transform trans)
    {
        Debug.Assert(renderType == MB_RenderType.skinnedMeshRenderer, "Render Type must be skinned mesh Renderer");
        Transform t = trans.parent;
        while (t != null)
        {
            if (Vector3.Distance(t.localScale, Vector3.one) > 10e-5f)
            {
                Debug.LogError("Src object " + trans.gameObject + " is a game object instance in the scene that is a child of a hierarchy with scale that is not (1,1,1). " +
                    "This object will become the bones of a skinned mesh renderer and these bones will be copied to the Combined Mesh Prefab. When this happens, it may not " +
                    "be possible to re create the bone position and scale in the Combined Mesh Prefab that matches the position and scale of the source object. /n/n" +
                    "When baking into a prefab it is recommended that all source objects be part of prefab instances in the scene. For best " +
                    " results create temporary prefabs if necessary and include all scaling in each prefab's hierarchy.");
            }
            t = t.parent;
        }
    }

    private static Transform _FindUppermostParent(GameObject go, Transform[] objsToCombinePrefabInstanceParent)
    {
        // traverse up to parent checking if any of the gameObjs are in the list of objs to combine.
        Transform commonParent = go.transform;
        Transform t = go.transform;
        while (t != null)
        {
            for (int i = 0; i < objsToCombinePrefabInstanceParent.Length; i++)
            {
                if (objsToCombinePrefabInstanceParent[i] == t) commonParent = objsToCombinePrefabInstanceParent[i];
            }
            t = t.parent;
        }

        return commonParent;
    }

    public static void SaveMeshsToAssetDatabase(MB3_MeshBakerCommon mom, string folderPath, string newFileNameBase)
    {
        if (MB3_MeshCombiner.EVAL_VERSION) return;
        if (mom is MB3_MeshBaker)
        {
            MB3_MeshBaker mb = (MB3_MeshBaker)mom;
            string newFilename = newFileNameBase + ".asset";
            string ap = AssetDatabase.GetAssetPath(((MB3_MeshCombinerSingle)mb.meshCombiner).GetMesh());
            if (ap == null || ap.Equals(""))
            {
                Debug.Log("Saving mesh asset to " + newFilename);
                AssetDatabase.CreateAsset(((MB3_MeshCombinerSingle)mb.meshCombiner).GetMesh(), newFilename);
            }
            else
            {
                Debug.Log("Mesh is an existing asset at " + ap);
            }
        }
        else if (mom is MB3_MultiMeshBaker)
        {
            MB3_MultiMeshBaker mmb = (MB3_MultiMeshBaker)mom;
            List<MB3_MultiMeshCombiner.CombinedMesh> combiners = ((MB3_MultiMeshCombiner)mmb.meshCombiner).meshCombiners;
            for (int i = 0; i < combiners.Count; i++)
            {
                string newFilename = newFileNameBase + i + ".asset";
                Mesh mesh = combiners[i].combinedMesh.GetMesh();
                string ap = AssetDatabase.GetAssetPath(mesh);
                if (ap == null || ap.Equals(""))
                {
                    Debug.Log("Saving mesh asset to " + newFilename);
                    AssetDatabase.CreateAsset(mesh, newFilename);
                }
                else
                {
                    Debug.Log("Mesh is an asset at " + ap);
                }
            }
        }
        else
        {
            Debug.LogError("Argument was not a MB3_MeshBaker or an MB3_MultiMeshBaker.");
        }
    }

    // The serialized object reference is necessary to work around a nasty unity bug.
    public static GameObject RebuildPrefab(MB3_MeshBakerCommon mom, ref SerializedObject so, bool leaveInstanceInSceneAfterBake, List<Transform> tempPrefabInstanceRoots, GameObject[] objsToCombine)
    {
        if (MB3_MeshCombiner.EVAL_VERSION) return null;

        if (mom.meshCombiner.LOG_LEVEL >= MB2_LogLevel.debug) Debug.Log("Rebuilding Prefab: " + mom.resultPrefab);
        GameObject prefabRoot = mom.resultPrefab;
        GameObject instanceRootGO = mom.meshCombiner.resultSceneObject;
        /*
        GameObject instanceRootGO = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot);
        instanceRootGO.transform.position = Vector3.zero;
        instanceRootGO.transform.rotation = Quaternion.identity;
        instanceRootGO.transform.localScale = Vector3.one;

        //remove everything in the prefab.
        
        MBVersionEditor.UnpackPrefabInstance(instanceRootGO, ref so);
        int numChildren = instanceRootGO.transform.childCount;
        for (int i = numChildren - 1; i >= 0; i--)
        {
            MB_Utility.Destroy(instanceRootGO.transform.GetChild(i).gameObject);
        }

        if (mom is MB3_MeshBaker)
        {
            MB3_MeshBaker mb = (MB3_MeshBaker)mom;
            MB3_MeshCombinerSingle mbs = (MB3_MeshCombinerSingle)mb.meshCombiner;
            MB3_MeshCombinerSingle.BuildPrefabHierarchy(mbs, instanceRootGO, mbs.GetMesh());
        }
        else if (mom is MB3_MultiMeshBaker)
        {
            MB3_MultiMeshBaker mmb = (MB3_MultiMeshBaker)mom;
            MB3_MultiMeshCombiner mbs = (MB3_MultiMeshCombiner)mmb.meshCombiner;
            for (int i = 0; i < mbs.meshCombiners.Count; i++)
            {
                MB3_MeshCombinerSingle.BuildPrefabHierarchy(mbs.meshCombiners[i].combinedMesh, instanceRootGO, mbs.meshCombiners[i].combinedMesh.GetMesh(), true);
            }
        }
        else
        {
            Debug.LogError("Argument was not a MB3_MeshBaker or an MB3_MultiMeshBaker.");
        }
        */

        if (mom.meshCombiner.settings.renderType == MB_RenderType.skinnedMeshRenderer)
        {
            _MoveBonesToCombinedMeshPrefabAndDeleteRenderers(instanceRootGO.transform, tempPrefabInstanceRoots, objsToCombine);
        }

        string prefabPth = AssetDatabase.GetAssetPath(prefabRoot);
        MBVersionEditor.PrefabUtility_ReplacePrefab(instanceRootGO, prefabPth, MB_ReplacePrefabOption.connectToPrefab);
        mom.resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPth);
        if (!leaveInstanceInSceneAfterBake)
        {
            Editor.DestroyImmediate(instanceRootGO);
        }

        return instanceRootGO;
    }

    public static void UnwrapUV2(Mesh mesh, float hardAngle, float packingMargin)
    {
        UnwrapParam up = new UnwrapParam();
        UnwrapParam.SetDefaults(out up);
        up.hardAngle = hardAngle;
        up.packMargin = packingMargin;
        Unwrapping.GenerateSecondaryUVSet(mesh, up);
    }

    public static bool _OkToCreateDummyTextureBakeResult(MB3_MeshBakerCommon mom)
    {
        List<GameObject> objsToMesh = mom.GetObjectsToCombine();
        if (objsToMesh.Count == 0)
            return false;
        return true;
    }
}
