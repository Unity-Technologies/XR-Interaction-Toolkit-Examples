//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    /// <summary>
    /// This class is an endless mesh. You don't need to worry about the 65k limit when adding meshes. It is like a List of combined meshes. Internally it manages
    /// a collection of MB2_MeshComber objects to which meshes added and deleted as necessary. 
    /// 
    /// Note that this implementation does
    /// not attempt to split meshes. Each mesh is added to one of the internal meshes as an atomic unit.
    /// 
    /// This class is not a Component so it can be instantiated and used like a regular C Sharp class.
    /// </summary>
    [System.Serializable]
    public class MB3_MultiMeshCombiner : MB3_MeshCombiner
    {

        [System.Serializable]
        public class CombinedMesh
        {
            public MB3_MeshCombinerSingle combinedMesh;
            public int extraSpace = -1;
            public int numVertsInListToDelete = 0;
            public int numVertsInListToAdd = 0;
            public List<GameObject> gosToAdd;
            public List<int> gosToDelete;
            public List<GameObject> gosToUpdate;
            public bool isDirty = false; //needs apply

            public CombinedMesh(int maxNumVertsInMesh, GameObject resultSceneObject, MB2_LogLevel ll)
            {
                combinedMesh = new MB3_MeshCombinerSingle();
                combinedMesh.resultSceneObject = resultSceneObject;
                combinedMesh.LOG_LEVEL = ll;
                extraSpace = maxNumVertsInMesh;
                numVertsInListToDelete = 0;
                numVertsInListToAdd = 0;
                gosToAdd = new List<GameObject>();
                gosToDelete = new List<int>();
                gosToUpdate = new List<GameObject>();
            }

            public bool isEmpty()
            {
                List<GameObject> obsIn = new List<GameObject>();
                obsIn.AddRange(combinedMesh.GetObjectsInCombined());
                for (int i = 0; i < gosToDelete.Count; i++)
                {
                    for (int j = 0; j < obsIn.Count; j++)
                    {
                        if (obsIn[j].GetInstanceID() == gosToDelete[i])
                        {
                            obsIn.RemoveAt(j);
                            break;
                        }
                    }

                }
                if (obsIn.Count == 0) return true;
                return false;
            }
        }

        static GameObject[] empty = new GameObject[0];
        static int[] emptyIDs = new int[0];

        public override MB2_LogLevel LOG_LEVEL
        {
            get { return _LOG_LEVEL; }
            set
            {
                _LOG_LEVEL = value;
                for (int i = 0; i < meshCombiners.Count; i++)
                {
                    meshCombiners[i].combinedMesh.LOG_LEVEL = value;
                }
            }
        }

        public override MB2_ValidationLevel validationLevel
        {
            set
            {
                _validationLevel = value;
                for (int i = 0; i < meshCombiners.Count; i++)
                {
                    meshCombiners[i].combinedMesh.validationLevel = _validationLevel;
                }
            }
            get { return _validationLevel; }
        }

        public Dictionary<int, CombinedMesh> obj2MeshCombinerMap = new Dictionary<int, CombinedMesh>();
        [SerializeField]
        public List<CombinedMesh> meshCombiners = new List<CombinedMesh>();

        [SerializeField]
        int _maxVertsInMesh = 65535;
        public int maxVertsInMesh
        {
            get { return _maxVertsInMesh; }
            set
            {
                if (obj2MeshCombinerMap.Count > 0)
                {
                    //todo how to warn with gui
                    //Debug.LogError("Can't set the max verts in meshes once there are objects in the mesh.");
                    return;
                }
                else if (value < 3)
                {
                    Debug.LogError("Max verts in mesh must be greater than three.");
                }
                else if (value > MBVersion.MaxMeshVertexCount())
                {
                    Debug.LogError("MultiMeshCombiner error in maxVertsInMesh. Meshes in unity cannot have more than " + MBVersion.MaxMeshVertexCount() + " vertices. " + value);
                }
                else
                {
                    _maxVertsInMesh = value;
                }
            }
        }

        public override int GetNumObjectsInCombined()
        {
            return obj2MeshCombinerMap.Count;
        }

        public override int GetNumVerticesFor(GameObject go)
        {
            CombinedMesh c = null;
            if (obj2MeshCombinerMap.TryGetValue(go.GetInstanceID(), out c))
            {
                return c.combinedMesh.GetNumVerticesFor(go);
            }
            else
            {
                return -1;
            }
        }

        public override int GetNumVerticesFor(int gameObjectID)
        {
            CombinedMesh c = null;
            if (obj2MeshCombinerMap.TryGetValue(gameObjectID, out c))
            {
                return c.combinedMesh.GetNumVerticesFor(gameObjectID);
            }
            else
            {
                return -1;
            }
        }

        public override List<GameObject> GetObjectsInCombined()
        { //todo look at getting from keys
            List<GameObject> allObjs = new List<GameObject>();
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                allObjs.AddRange(meshCombiners[i].combinedMesh.GetObjectsInCombined());
            }
            return allObjs;
        }

        public override int GetLightmapIndex()
        { //todo check that all meshcombiners use same lightmap index
            if (meshCombiners.Count > 0) return meshCombiners[0].combinedMesh.GetLightmapIndex();
            return -1;
        }

        public override bool CombinedMeshContains(GameObject go)
        {
            return obj2MeshCombinerMap.ContainsKey(go.GetInstanceID());
        }

        bool _validateTextureBakeResults()
        {
            if (_textureBakeResults == null)
            {
                Debug.LogError("Texture Bake Results is null. Can't combine meshes.");
                return false;
            }
            if ((_textureBakeResults.materialsAndUVRects == null || _textureBakeResults.materialsAndUVRects.Length == 0))
            {
                Debug.LogError("Texture Bake Results has no materials in material to sourceUVRect map. Try baking materials. Can't combine meshes. " +
                     "If you are trying to combine meshes without combining materials, try removing the Texture Bake Result.");
                return false;
            }


            if (_textureBakeResults.NumResultMaterials() == 0)
            {
                Debug.LogError("Texture Bake Results has no result materials. Try baking materials. Can't combine meshes.");
                return false;
            }

            return true;
        }

        public override void Apply(MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod)
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                if (meshCombiners[i].isDirty)
                {
                    meshCombiners[i].combinedMesh.Apply(uv2GenerationMethod);
                    meshCombiners[i].isDirty = false;
                }
            }
        }

        public override void Apply(bool triangles, bool vertices, bool normals, bool tangents, bool uvs, bool uv2, bool uv3, bool uv4, bool colors, bool bones = false, bool blendShapeFlag = false, GenerateUV2Delegate uv2GenerationMethod = null)
        {
            Apply(triangles, vertices, normals, tangents,
                uvs, uv2, uv3, uv4,
                false, false, false, false,
                colors, bones, blendShapeFlag, uv2GenerationMethod);
        }

        public override void Apply(bool triangles,
                          bool vertices,
                          bool normals,
                          bool tangents,
                          bool uvs,
                          bool uv2,
                          bool uv3,
                          bool uv4,
                          bool uv5,
                          bool uv6,
                          bool uv7,
                          bool uv8,
                          bool colors,
                          bool bones = false,
                          bool blendShapesFlag = false,
                          MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod = null)
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                if (meshCombiners[i].isDirty)
                {
                    meshCombiners[i].combinedMesh.Apply(triangles, vertices, normals, tangents, uvs, uv2, uv3, uv4, colors, bones, blendShapesFlag, uv2GenerationMethod);
                    meshCombiners[i].isDirty = false;
                }
            }
        }

        public override void UpdateSkinnedMeshApproximateBounds()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.UpdateSkinnedMeshApproximateBounds();
            }
        }

        public override void UpdateSkinnedMeshApproximateBoundsFromBones()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.UpdateSkinnedMeshApproximateBoundsFromBones();
            }
        }

        public override void UpdateSkinnedMeshApproximateBoundsFromBounds()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.UpdateSkinnedMeshApproximateBoundsFromBounds();
            }
        }

        public override bool UpdateGameObjects(GameObject[] gos, bool recalcBounds,
                                        bool updateVertices, bool updateNormals, bool updateTangents,
                                        bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4,
                                        bool updateColors, bool updateSkinningInfo)
        {
            return UpdateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents,
                updateUV, updateUV2, updateUV3, updateUV4, false, false, false, false,
                updateColors, updateSkinningInfo);
        }

        public override bool UpdateGameObjects(GameObject[] gos, bool recalcBounds,
                                        bool updateVertices, bool updateNormals, bool updateTangents,
                                        bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4,
                                        bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8,
                                        bool updateColors, bool updateSkinningInfo)
        {
            if (gos == null)
            {
                Debug.LogError("list of game objects cannot be null");
                return false;
            }

            //build gos lists
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].gosToUpdate.Clear();
            }

            for (int i = 0; i < gos.Length; i++)
            {
                CombinedMesh cm = null;
                obj2MeshCombinerMap.TryGetValue(gos[i].GetInstanceID(), out cm);
                if (cm != null)
                {
                    cm.gosToUpdate.Add(gos[i]);
                }
                else
                {
                    Debug.LogWarning("Object " + gos[i] + " is not in the combined mesh.");
                }
            }

            bool success = true;
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                if (meshCombiners[i].gosToUpdate.Count > 0)
                {
                    meshCombiners[i].isDirty = true;
                    GameObject[] gosToUpdate = meshCombiners[i].gosToUpdate.ToArray();
                    success = success && meshCombiners[i].combinedMesh.UpdateGameObjects(gosToUpdate, recalcBounds, updateVertices, updateNormals, updateTangents,
                        updateUV, updateUV2, updateUV3, updateUV4, updateUV5, updateUV6, updateUV7, updateUV8,
                        updateColors, updateSkinningInfo);
                }
            }

            return success;
        }

        public override bool AddDeleteGameObjects(GameObject[] gos, GameObject[] deleteGOs, bool disableRendererInSource = true)
        {
            int[] delInstanceIDs = null;
            if (deleteGOs != null)
            {
                delInstanceIDs = new int[deleteGOs.Length];
                for (int i = 0; i < deleteGOs.Length; i++)
                {
                    if (deleteGOs[i] == null)
                    {
                        Debug.LogError("The " + i + "th object on the list of objects to delete is 'Null'");
                    }
                    else
                    {
                        delInstanceIDs[i] = deleteGOs[i].GetInstanceID();
                    }
                }
            }
            return AddDeleteGameObjectsByID(gos, delInstanceIDs, disableRendererInSource);
        }

        public override bool AddDeleteGameObjectsByID(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource = true)
        {
            //Profile.Start//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects1");
            //PART 1 ==== Validate
            if (deleteGOinstanceIDs == null) deleteGOinstanceIDs = emptyIDs;
            if (_usingTemporaryTextureBakeResult && gos != null && gos.Length > 0)
            {
                MB_Utility.Destroy(_textureBakeResults);
                _textureBakeResults = null;
                _usingTemporaryTextureBakeResult = false;
            }

            //if all objects use the same material we can create a temporary _textureBakeResults 
            if (_textureBakeResults == null && gos != null && gos.Length > 0 && gos[0] != null)
            {
                if (!_CreateTemporaryTextrueBakeResult(gos, GetMaterialsOnTargetRenderer()))
                {
                    return false;
                }
            }

            if (!_validate(gos, deleteGOinstanceIDs))
            {
                return false;
            }
            _distributeAmongBakers(gos, deleteGOinstanceIDs);
            if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("MB2_MultiMeshCombiner.AddDeleteGameObjects numCombinedMeshes: " + meshCombiners.Count + " added:" + gos.Length + " deleted:" + deleteGOinstanceIDs.Length + " disableRendererInSource:" + disableRendererInSource + " maxVertsPerCombined:" + _maxVertsInMesh);
            return _bakeStep1(gos, deleteGOinstanceIDs, disableRendererInSource);
        }

        bool _validate(GameObject[] gos, int[] deleteGOinstanceIDs)
        {
            if (_validationLevel == MB2_ValidationLevel.none) return true;
            if (_maxVertsInMesh < 3) Debug.LogError("Invalid value for maxVertsInMesh=" + _maxVertsInMesh);
            _validateTextureBakeResults();

            if (gos != null)
            {
                for (int i = 0; i < gos.Length; i++)
                {
                    if (gos[i] == null)
                    {
                        Debug.LogError("The " + i + "th object on the list of objects to combine is 'None'. Use Command-Delete on Mac OS X; Delete or Shift-Delete on Windows to remove this one element.");
                        return false;
                    }
                    if (_validationLevel >= MB2_ValidationLevel.robust)
                    {
                        for (int j = i + 1; j < gos.Length; j++)
                        {
                            if (gos[i] == gos[j])
                            {
                                Debug.LogError("GameObject " + gos[i] + "appears twice in list of game objects to add");
                                return false;
                            }
                        }
                        if (obj2MeshCombinerMap.ContainsKey(gos[i].GetInstanceID()))
                        {
                            bool isInDeleteList = false;
                            if (deleteGOinstanceIDs != null)
                            {
                                for (int k = 0; k < deleteGOinstanceIDs.Length; k++)
                                {
                                    if (deleteGOinstanceIDs[k] == gos[i].GetInstanceID()) isInDeleteList = true;
                                }
                            }
                            if (!isInDeleteList)
                            {
                                Debug.LogError("GameObject " + gos[i] + " is already in the combined mesh " + gos[i].GetInstanceID());
                                return false;
                            }
                        }
                    }
                }
            }
            if (deleteGOinstanceIDs != null)
            {
                if (_validationLevel >= MB2_ValidationLevel.robust)
                {
                    for (int i = 0; i < deleteGOinstanceIDs.Length; i++)
                    {
                        for (int j = i + 1; j < deleteGOinstanceIDs.Length; j++)
                        {
                            if (deleteGOinstanceIDs[i] == deleteGOinstanceIDs[j])
                            {
                                Debug.LogError("GameObject " + deleteGOinstanceIDs[i] + "appears twice in list of game objects to delete");
                                return false;
                            }
                        }
                        if (!obj2MeshCombinerMap.ContainsKey(deleteGOinstanceIDs[i]))
                        {
                            Debug.LogWarning("GameObject with instance ID " + deleteGOinstanceIDs[i] + " on the list of objects to delete is not in the combined mesh.");
                        }
                    }
                }
            }
            return true;
        }

        void _distributeAmongBakers(GameObject[] gos, int[] deleteGOinstanceIDs)
        {
            if (gos == null) gos = empty;
            if (deleteGOinstanceIDs == null) deleteGOinstanceIDs = emptyIDs;

            if (resultSceneObject == null) resultSceneObject = new GameObject("CombinedMesh-" + name);

            //PART 2 ==== calculate which bakers to add objects to
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].extraSpace = _maxVertsInMesh - meshCombiners[i].combinedMesh.GetMesh().vertexCount;
            }
            //Profile.End//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects1");

            //Profile.Start//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects2.1");		
            //first delete game objects from the existing combinedMeshes keep track of free space
            for (int i = 0; i < deleteGOinstanceIDs.Length; i++)
            {
                CombinedMesh c = null;
                if (obj2MeshCombinerMap.TryGetValue(deleteGOinstanceIDs[i], out c))
                {
                    if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("MB2_MultiMeshCombiner.Removing " + deleteGOinstanceIDs[i] + " from meshCombiner " + meshCombiners.IndexOf(c));
                    c.numVertsInListToDelete += c.combinedMesh.GetNumVerticesFor(deleteGOinstanceIDs[i]);  //m.vertexCount;
                    c.gosToDelete.Add(deleteGOinstanceIDs[i]);
                }
                else
                {
                    Debug.LogWarning("Object " + deleteGOinstanceIDs[i] + " in the list of objects to delete is not in the combined mesh.");
                }
            }
            for (int i = 0; i < gos.Length; i++)
            {
                GameObject go = gos[i];
                int numVerts = MB_Utility.GetMesh(go).vertexCount;
                CombinedMesh cm = null;
                for (int j = 0; j < meshCombiners.Count; j++)
                {
                    if (meshCombiners[j].extraSpace + meshCombiners[j].numVertsInListToDelete - meshCombiners[j].numVertsInListToAdd > numVerts)
                    {
                        cm = meshCombiners[j];
                        if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("MB2_MultiMeshCombiner.Added " + gos[i] + " to combinedMesh " + j, LOG_LEVEL);
                        break;
                    }
                }
                if (cm == null)
                {
                    cm = new CombinedMesh(maxVertsInMesh, _resultSceneObject, _LOG_LEVEL);
                    _setMBValues(cm.combinedMesh);
                    meshCombiners.Add(cm);
                    if (LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("MB2_MultiMeshCombiner.Created new combinedMesh");
                }
                cm.gosToAdd.Add(go);
                cm.numVertsInListToAdd += numVerts;
                //			obj2MeshCombinerMap.Add(go,cm);
            }
        }

        bool _bakeStep1(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource)
        {
            //Profile.End//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects2.2");	
            //Profile.Start//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects3"); 
            //PART 3 ==== Add delete meshes from combined
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                CombinedMesh cm = meshCombiners[i];
                if (cm.combinedMesh.targetRenderer == null)
                {
                    cm.combinedMesh.resultSceneObject = _resultSceneObject;
                    cm.combinedMesh.BuildSceneMeshObject(gos, true);
                    if (_LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("BuildSO combiner {0} goID {1} targetRenID {2} meshID {3}", i, cm.combinedMesh.targetRenderer.gameObject.GetInstanceID(), cm.combinedMesh.targetRenderer.GetInstanceID(), cm.combinedMesh.GetMesh().GetInstanceID());

                }
                else
                {
                    if (cm.combinedMesh.targetRenderer.transform.parent != resultSceneObject.transform)
                    {
                        Debug.LogError("targetRender objects must be children of resultSceneObject");
                        return false;
                    }
                }
                if (cm.gosToAdd.Count > 0 || cm.gosToDelete.Count > 0)
                {
                    cm.combinedMesh.AddDeleteGameObjectsByID(cm.gosToAdd.ToArray(), cm.gosToDelete.ToArray(), disableRendererInSource);
                    if (_LOG_LEVEL >= MB2_LogLevel.debug) MB2_Log.LogDebug("Baked combiner {0} obsAdded {1} objsRemoved {2} goID {3} targetRenID {4} meshID {5}", i, cm.gosToAdd.Count, cm.gosToDelete.Count, cm.combinedMesh.targetRenderer.gameObject.GetInstanceID(), cm.combinedMesh.targetRenderer.GetInstanceID(), cm.combinedMesh.GetMesh().GetInstanceID());
                }
                Renderer r = cm.combinedMesh.targetRenderer;
                Mesh m = cm.combinedMesh.GetMesh();
                if (r is MeshRenderer)
                {
                    MeshFilter mf = r.gameObject.GetComponent<MeshFilter>();
                    mf.sharedMesh = m;
                }
                else
                {
                    SkinnedMeshRenderer smr = (SkinnedMeshRenderer)r;
                    smr.sharedMesh = m;
                }
            }
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                CombinedMesh cm = meshCombiners[i];
                for (int j = 0; j < cm.gosToDelete.Count; j++)
                {
                    obj2MeshCombinerMap.Remove(cm.gosToDelete[j]);
                }
            }
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                CombinedMesh cm = meshCombiners[i];
                for (int j = 0; j < cm.gosToAdd.Count; j++)
                {
                    obj2MeshCombinerMap.Add(cm.gosToAdd[j].GetInstanceID(), cm);
                }
                if (cm.gosToAdd.Count > 0 || cm.gosToDelete.Count > 0)
                {
                    cm.gosToDelete.Clear();
                    cm.gosToAdd.Clear();
                    cm.numVertsInListToDelete = 0;
                    cm.numVertsInListToAdd = 0;
                    cm.isDirty = true;
                }
            }
            //Profile.End//Profile("MB2_MultiMeshCombiner.AddDeleteGameObjects3"); 
            if (LOG_LEVEL >= MB2_LogLevel.debug)
            {
                string s = "Meshes in combined:";
                for (int i = 0; i < meshCombiners.Count; i++)
                {
                    s += " mesh" + i + "(" + meshCombiners[i].combinedMesh.GetObjectsInCombined().Count + ")\n";
                }
                s += "children in result: " + resultSceneObject.transform.childCount;
                MB2_Log.LogDebug(s, LOG_LEVEL);
            }
            if (meshCombiners.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [System.Obsolete("BuildSourceBlendShapeToCombinedIndexMap is deprecated. The map will be attached to the combined SkinnedMeshRenderer objects as the MB_BlendShape2CombinedMap Component.")]
        public override Dictionary<MBBlendShapeKey, MBBlendShapeValue> BuildSourceBlendShapeToCombinedIndexMap()
        {
            Dictionary<MBBlendShapeKey, MBBlendShapeValue> map = new Dictionary<MBBlendShapeKey, MBBlendShapeValue>();
            for (int combinerIdx = 0; combinerIdx < meshCombiners.Count; combinerIdx++)
            {

                if (meshCombiners[combinerIdx].combinedMesh.targetRenderer == null) continue;
                MB_BlendShape2CombinedMap mapComponent = meshCombiners[combinerIdx].combinedMesh.targetRenderer.GetComponent<MB_BlendShape2CombinedMap>();
                if (mapComponent == null) continue;
                foreach (KeyValuePair<MBBlendShapeKey, MBBlendShapeValue> entry in mapComponent.srcToCombinedMap.GenerateMapFromSerializedData())
                {
                    map.Add(entry.Key, entry.Value);
                }
            }

            return map;
        }

        public override void ClearBuffers()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.ClearBuffers();
            }
            obj2MeshCombinerMap.Clear();
        }

        public override void ClearMesh()
        {
            // For the MultiMeshCombiner we want to destroy because that is what an "empty" multi mesh combiner looks like.
            DestroyMesh();
        }

        public override void ClearMesh(MB2_EditorMethodsInterface editorMethods)
        {
            DestroyMeshEditor(editorMethods);
        }

        public override void DisposeRuntimeCreated()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.DisposeRuntimeCreated();
            }
        }

        public override void DestroyMesh()
        {
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                if (meshCombiners[i].combinedMesh.targetRenderer != null)
                {
                    MB_Utility.Destroy(meshCombiners[i].combinedMesh.targetRenderer.gameObject);
                }
                
                meshCombiners[i].combinedMesh.DestroyMesh();
            }

            obj2MeshCombinerMap.Clear();
            meshCombiners.Clear();
        }

        public override void DestroyMeshEditor(MB2_EditorMethodsInterface editorMethods)
        {
            editorMethods.Destroy(resultSceneObject);
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                //if (meshCombiners[i].combinedMesh.targetRenderer != null)
                //{
                //    editorMethods.Destroy(meshCombiners[i].combinedMesh.targetRenderer.gameObject);
                //}

                meshCombiners[i].combinedMesh.ClearMesh();
            }

            obj2MeshCombinerMap.Clear();
            meshCombiners.Clear();
        }

        void _setMBValues(MB3_MeshCombinerSingle targ)
        {
            targ.validationLevel = _validationLevel;
            targ.textureBakeResults = textureBakeResults;
            
            // Even though the MultiMeshBaker supports baking into prefabs, the sub-combiners don't do bake into prefab when
            // this is happening. They do bake into sceneObject, then the MultiMeshBaker takes their output and combines it
            // into a prefab.
            targ.outputOption = MB2_OutputOptions.bakeIntoSceneObject;
            if (settingsHolder != null)
            {
                targ.settingsHolder = settingsHolder;
            } else 
            {
                targ.renderType = renderType;
                targ.lightmapOption = lightmapOption;
                targ.doNorm = doNorm;
                targ.doTan = doTan;
                targ.doCol = doCol;
                targ.doUV = doUV;
                targ.doUV3 = doUV3;
                targ.doUV4 = doUV4;
                targ.doUV5 = doUV5;
                targ.doUV6 = doUV6;
                targ.doUV7 = doUV7;
                targ.doUV8 = doUV8;
                targ.doBlendShapes = doBlendShapes;
                targ.optimizeAfterBake = optimizeAfterBake;
                targ.pivotLocationType = pivotLocationType;
                targ.uv2UnwrappingParamsHardAngle = uv2UnwrappingParamsHardAngle;
                targ.uv2UnwrappingParamsPackMargin = uv2UnwrappingParamsPackMargin;
                targ.assignToMeshCustomizer = assignToMeshCustomizer;
            }
        }

        public override List<Material> GetMaterialsOnTargetRenderer()
        {
            HashSet<Material> hs = new HashSet<Material>();
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                hs.UnionWith(meshCombiners[i].combinedMesh.GetMaterialsOnTargetRenderer());
            }
            List<Material> outMats = new List<Material>(hs);
            return outMats;
        }

        public override void CheckIntegrity()
        {
            if (!MB_Utility.DO_INTEGRITY_CHECKS) return;
            for (int i = 0; i < meshCombiners.Count; i++)
            {
                meshCombiners[i].combinedMesh.CheckIntegrity();
            }
        }
    }
}