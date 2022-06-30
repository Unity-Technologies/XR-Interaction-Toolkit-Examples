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


/// <summary>
/// Maps a list of source materials to a combined material. Included in MB2_TextureBakeResults
/// </summary>


/// <summary>
/// Abstract root of the mesh combining classes
/// </summary>
public abstract class MB3_MeshBakerCommon : MB3_MeshBakerRoot {

    //todo should be list of <Renderer>
    public List<GameObject> objsToMesh;

    public abstract MB3_MeshCombiner meshCombiner {
        get;
    }

    public bool useObjsToMeshFromTexBaker = true;

    public bool clearBuffersAfterBake = true;

    //t0do put this in the batch baker
    public string bakeAssetsInPlaceFolderPath;

    [HideInInspector] public GameObject resultPrefab;

    /// <summary>
    /// If checked then an instance will be left in the scene after baking. Otherwise scene instance will be deleted after prefab is baked.
    /// </summary>
    [HideInInspector] public bool resultPrefabLeaveInstanceInSceneAfterBake;

    /// <summary>
    /// Optional, combined mesh renderers will be children of this object if it exists.
    /// </summary>
    [HideInInspector] public Transform parentSceneObject;

#if UNITY_EDITOR
    [ContextMenu("Create Mesh Baker Settings Asset")]
    public void CreateMeshBakerSettingsAsset()
    {

        string newFilePath = UnityEditor.EditorUtility.SaveFilePanelInProject("New Mesh Baker Settings", "MeshBakerSettings", "asset", "Create a new Mesh Baker Settings Asset");
        if (newFilePath != null)
        {
            MB3_MeshCombinerSettings asset = ScriptableObject.CreateInstance<MB3_MeshCombinerSettings>();
            UnityEditor.AssetDatabase.CreateAsset(asset, newFilePath);
        }
    }

    [ContextMenu("Copy settings from Shared Settings")]
    public void CopyMySettingsToAssignedSettingsAsset()
    {
        if (meshCombiner.settingsHolder == null)
        {
            Debug.LogError("No Shared Settings Asset Assigned.");
            return;
        }

        UnityEditor.Undo.RecordObject(this, "Undo copy settings");
        _CopySettings(meshCombiner.settingsHolder.GetMeshBakerSettings(), meshCombiner);
        Debug.Log("Copied settings from assigned Shared Settings to this Mesh Baker.");
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Copy settings to Shared Settings")]
    public void CopyAssignedSettingsAssetToMySettings()
    {
        if (meshCombiner.settingsHolder == null)
        {
            Debug.LogError("No Shared Settings Asset Assigned.");
            return;
        }

        if (meshCombiner.settingsHolder is UnityEngine.Object) UnityEditor.Undo.RecordObject((UnityEngine.Object)meshCombiner.settingsHolder, "Undo copy settings");
        _CopySettings(meshCombiner, meshCombiner.settingsHolder.GetMeshBakerSettings());
        Debug.Log("Copied settings from this Mesh Baker to the assigned Shared Settings asset.");
        if (meshCombiner.settingsHolder is UnityEngine.Object) UnityEditor.EditorUtility.SetDirty((UnityEngine.Object)meshCombiner.settingsHolder);
    }

    void _CopySettings(MB_IMeshBakerSettings src, MB_IMeshBakerSettings targ)
    {
        targ.clearBuffersAfterBake = src.clearBuffersAfterBake;
        targ.doBlendShapes = src.doBlendShapes;
        targ.doCol = src.doCol;
        targ.doNorm = src.doNorm;
        targ.doTan = src.doTan;
        targ.doUV = src.doUV;
        targ.doUV3 = src.doUV3;
        targ.doUV4 = src.doUV4;
        targ.doUV5 = src.doUV5;
        targ.doUV6 = src.doUV6;
        targ.doUV7 = src.doUV7;
        targ.doUV8 = src.doUV8;
        targ.optimizeAfterBake = src.optimizeAfterBake;
        targ.pivotLocationType = src.pivotLocationType;
        targ.lightmapOption = src.lightmapOption;
        targ.renderType = src.renderType;
        targ.uv2UnwrappingParamsHardAngle = src.uv2UnwrappingParamsHardAngle;
        targ.uv2UnwrappingParamsPackMargin = src.uv2UnwrappingParamsPackMargin;
    }
#endif

    public override MB2_TextureBakeResults textureBakeResults {
        get { return meshCombiner.textureBakeResults; }
        set { meshCombiner.textureBakeResults = value; }
    }

    public override List<GameObject> GetObjectsToCombine() {
        if (useObjsToMeshFromTexBaker) {
            MB3_TextureBaker tb = gameObject.GetComponent<MB3_TextureBaker>();
            if (tb == null) tb = gameObject.transform.parent.GetComponent<MB3_TextureBaker>();
            if (tb != null) {
                return tb.GetObjectsToCombine();
            } else {
                Debug.LogWarning("Use Objects To Mesh From Texture Baker was checked but no texture baker");
                return new List<GameObject>();
            }
        } else {
            if (objsToMesh == null) objsToMesh = new List<GameObject>();
            return objsToMesh;
        }
    }

    [ContextMenu("Purge Objects to Combine of null references")]
    public override void PurgeNullsFromObjectsToCombine()
    {
        if (useObjsToMeshFromTexBaker)
        {
            MB3_TextureBaker tb = gameObject.GetComponent<MB3_TextureBaker>();
            if (tb == null)
            {
                tb = gameObject.transform.parent.GetComponent<MB3_TextureBaker>();
            }
            if (tb != null)
            {
                tb.PurgeNullsFromObjectsToCombine();
            }
            else
            {
                Debug.LogWarning("Use Objects To Mesh From Texture Baker was checked but no texture baker, could not purge");
            }
        }
        else
        {
            if (objsToMesh == null)
            {
                objsToMesh = new List<GameObject>();
            }
            Debug.Log(string.Format("Purged {0} null references from objects to combine list.", objsToMesh.RemoveAll(obj => obj == null)));
        }
    }

    public void EnableDisableSourceObjectRenderers(bool show) {
        for (int i = 0; i < GetObjectsToCombine().Count; i++) {
            GameObject go = GetObjectsToCombine()[i];
            if (go != null) {
                Renderer mr = MB_Utility.GetRenderer(go);
                if (mr != null) {
                    mr.enabled = show;
                }

                LODGroup lodG = mr.GetComponentInParent<LODGroup>();
                if (lodG != null)
                {
                    bool isOnlyInGroup = true;
                    LOD[] lods = lodG.GetLODs();
                    for (int j = 0; j < lods.Length; j++)
                    {
                        for (int k = 0; k < lods[j].renderers.Length; k++)
                        {
                            if (lods[j].renderers[k] != mr)
                            {
                                isOnlyInGroup = false;
                                break;
                            }
                        }
                    }

                    if (isOnlyInGroup)
                    {
                        lodG.enabled = show;
                    }
                }
            }
        }
    }

    /// <summary>
    ///  Clears the meshs and mesh related data but does not destroy it.
    /// </summary>
    public virtual void ClearMesh() 
    {
        meshCombiner.ClearMesh();
    }

    public virtual void ClearMesh(MB2_EditorMethodsInterface editorMethods)
    {
        meshCombiner.ClearMesh(editorMethods);
    }

    /// <summary>
    ///  Clears and desroys the mesh. Clears mesh related data.
    /// </summary>		
    public virtual void DestroyMesh(){
		meshCombiner.DestroyMesh();
	}

	public virtual void DestroyMeshEditor(MB2_EditorMethodsInterface editorMethods){
		meshCombiner.DestroyMeshEditor(editorMethods);
	}	

	public virtual int GetNumObjectsInCombined(){
		return meshCombiner.GetNumObjectsInCombined();	
	}
	
	public virtual int GetNumVerticesFor(GameObject go){
		return meshCombiner.GetNumVerticesFor(go);
	}

	/// <summary>
	/// Gets the texture baker on this component or its parent if it exists
	/// </summary>
	/// <returns>The texture baker.</returns>
	public MB3_TextureBaker GetTextureBaker(){
		MB3_TextureBaker tb = GetComponent<MB3_TextureBaker>();
		if (tb != null) return tb;
		if (transform.parent != null) return transform.parent.GetComponent<MB3_TextureBaker>();
		return null;
	}

/// <summary>
/// Adds and deletes objects from the combined mesh. gos and deleteGOs can be null. 
/// You need to call Apply or ApplyAll to see the changes. 
/// objects in gos must not include objects already in the combined mesh.
/// objects in gos and deleteGOs must be the game objects with a Renderer component
/// This method is slow, so should be called as infrequently as possible.
/// </summary>
/// <returns>
/// The first generated combined mesh
/// </returns>
/// <param name='gos'>
/// gos. Array of objects to add to the combined mesh. Array can be null. Must not include objects
/// already in the combined mesh. Array must contain game objects with a render component.
/// </param>
/// <param name='deleteGOs'>
/// deleteGOs. Array of objects to delete from the combined mesh. Array can be null.
/// </param>
/// <param name='disableRendererInSource'>
/// Disable renderer component on objects in gos after they have been added to the combined mesh.
/// </param>
/// <param name='fixOutOfBoundUVs'>
/// Whether to fix out of bounds UVs in meshes as they are being added. This paramater should be set to the same as the combined material.
/// </param>
/// </summary>
	public abstract bool AddDeleteGameObjects(GameObject[] gos, GameObject[] deleteGOs, bool disableRendererInSource = true);
	
	/// <summary>
	/// This is the best version to use for deleting game objects since the source GameObjects may have been destroyed
	/// Internaly Mesh Baker only stores the instanceID for Game Objects, so objects can be removed after they have been destroyed
	/// </summary>
	public abstract bool AddDeleteGameObjectsByID(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource = true);	
	
/// <summary>
/// Apply changes to the mesh. All channels set in this instance will be set in the combined mesh.
/// </summary>	
	public virtual void Apply(MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod=null){
		meshCombiner.name = name + "-mesh";
		meshCombiner.Apply(uv2GenerationMethod);
        if (parentSceneObject != null && meshCombiner.resultSceneObject != null)
        {
            meshCombiner.resultSceneObject.transform.parent = parentSceneObject;
        }
    }

/// <summary>	
/// Applys the changes to flagged properties of the mesh. This method is slow, and should only be called once per frame. The speed is directly proportional to the number of flags that are true. Only apply necessary properties.	
/// </summary>	
	public virtual void Apply(bool triangles,
					  bool vertices,
					  bool normals,
					  bool tangents,
					  bool uvs,
					  bool uv2,
					  bool uv3,
                      bool uv4,
					  bool colors,
					  bool bones=false,
                      bool blendShapesFlag=false,
					  MB3_MeshCombiner.GenerateUV2Delegate uv2GenerationMethod=null){
		meshCombiner.name = name + "-mesh";
		meshCombiner.Apply(triangles,vertices,normals,tangents,uvs,uv2,uv3,uv4,colors,bones, blendShapesFlag,uv2GenerationMethod);
        if (parentSceneObject != null && meshCombiner.resultSceneObject != null)
        {
            meshCombiner.resultSceneObject.transform.parent = parentSceneObject;
        }
    }	
	
	public virtual bool CombinedMeshContains(GameObject go){
		return meshCombiner.CombinedMeshContains(go);
	}

    /// <summary>
    /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
    /// This is faster than adding and removing a mesh and has a much lower memory footprint.
    /// This method can only be used if the meshes being updated have the same layout(number of 
    /// vertices, triangles, submeshes).
    /// This is faster than removing and re-adding
    /// For efficiency update as few channels as possible.
    /// Apply must be called to apply the changes to the combined mesh
    /// </summary>		
    public virtual void UpdateGameObjects(GameObject[] gos)
    {
        meshCombiner.name = name + "-mesh";
        meshCombiner.UpdateGameObjects(gos, true, true, true, true, true,
            false,false,false,false,false,false,false,false,false);
    }

    /// <summary>
    /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
    /// This is faster than adding and removing a mesh and has a much lower memory footprint.
    /// This method can only be used if the meshes being updated have the same layout(number of 
    /// vertices, triangles, submeshes).
    /// This is faster than removing and re-adding
    /// For efficiency update as few channels as possible.
    /// Apply must be called to apply the changes to the combined mesh
    /// </summary>		
    public virtual void UpdateGameObjects(GameObject[] gos, bool updateBounds)
    {
        meshCombiner.name = name + "-mesh";
        meshCombiner.UpdateGameObjects(gos, true, true, true, true, true,
            false, false, false, false, false, false, false, false, false);
    }

    /// <summary>
    /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
    /// This is faster than adding and removing a mesh and has a much lower memory footprint.
    /// This method can only be used if the meshes being updated have the same layout(number of 
    /// vertices, triangles, submeshes).
    /// This is faster than removing and re-adding
    /// For efficiency update as few channels as possible.
    /// Apply must be called to apply the changes to the combined mesh
    /// </summary>		
    public virtual void UpdateGameObjects(GameObject[] gos, bool recalcBounds, bool updateVertices, bool updateNormals, bool updateTangents,
									    bool updateUV, bool updateUV1, bool updateUV2,
										bool updateColors, bool updateSkinningInfo){
		meshCombiner.name = name + "-mesh";
		meshCombiner.UpdateGameObjects(gos,recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, false, false, updateColors, updateSkinningInfo);
	}

    /// <summary>
    /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
    /// This is faster than adding and removing a mesh and has a much lower memory footprint.
    /// This method can only be used if the meshes being updated have the same layout(number of 
    /// vertices, triangles, submeshes).
    /// This is faster than removing and re-adding
    /// For efficiency update as few channels as possible.
    /// Apply must be called to apply the changes to the combined mesh
    /// </summary>		
    public virtual bool UpdateGameObjects(GameObject[] gos, bool recalcBounds,
                                    bool updateVertices, bool updateNormals, bool updateTangents,
                                    bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4,
                                    bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8,
                                    bool updateColors, bool updateSkinningInfo)
    {
        meshCombiner.name = name + "-mesh";
        return meshCombiner.UpdateGameObjects(gos, recalcBounds, updateVertices, updateNormals, updateTangents, updateUV, updateUV2, updateUV3, updateUV4, updateUV5, updateUV6, updateUV7, updateUV8, updateColors, updateSkinningInfo);
    }


    public virtual void UpdateSkinnedMeshApproximateBounds(){
		if (_ValidateForUpdateSkinnedMeshBounds()){
			meshCombiner.UpdateSkinnedMeshApproximateBounds();
		}
	}

	public virtual void UpdateSkinnedMeshApproximateBoundsFromBones(){
		if (_ValidateForUpdateSkinnedMeshBounds()){
			meshCombiner.UpdateSkinnedMeshApproximateBoundsFromBones();
		}
	}

	public virtual void UpdateSkinnedMeshApproximateBoundsFromBounds(){
		if (_ValidateForUpdateSkinnedMeshBounds()){
			meshCombiner.UpdateSkinnedMeshApproximateBoundsFromBounds();
		}
	}

	protected virtual bool _ValidateForUpdateSkinnedMeshBounds(){
		if (meshCombiner.outputOption == MB2_OutputOptions.bakeMeshAssetsInPlace){
			Debug.LogWarning("Can't UpdateSkinnedMeshApproximateBounds when output type is bakeMeshAssetsInPlace");
			return false;
		}
		if (meshCombiner.resultSceneObject == null){
			Debug.LogWarning("Result Scene Object does not exist. No point in calling UpdateSkinnedMeshApproximateBounds.");
			return false;			
		}
		SkinnedMeshRenderer smr = meshCombiner.resultSceneObject.GetComponentInChildren<SkinnedMeshRenderer>();	
		if (smr == null){
			Debug.LogWarning("No SkinnedMeshRenderer on result scene object.");
			return false;			
		}
		return true;
	}	
}
