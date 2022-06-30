//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using DigitalOpus.MB.Core;
using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{

    [CustomEditor(typeof(MB3_MeshBakerGrouper))]
    [CanEditMultipleObjects]
    public class MB3_MeshBakerGrouperEditor : Editor
    {

        long lastBoundsCheckRefreshTime = 0;

        static GUIContent gc_ClusterType = new GUIContent("Cluster Type", "The scene will be divided into cells. Meshes in each cell will be grouped into a single mesh baker");
        static GUIContent gc_GridOrigin = new GUIContent("Origin", "The scene will be divided into cells. Meshes in each cell will be grouped into a single baker. This sets the origin for the clustering.");
        static GUIContent gc_CellSize = new GUIContent("Cell Size", "The scene will be divided into a grid of cells. Meshes in each cell will be grouped into a single baker. This sets the size of the cells.");
        static GUIContent gc_ClusterOnLMIndex = new GUIContent("Group By Lightmap Index", "Meshes sharing a lightmap index will be grouped together.");
        static GUIContent gc_NumSegements = new GUIContent("Num Pie Segments", "Number of segments/slices in the pie.");
        static GUIContent gc_PieAxis = new GUIContent("Pie Axis", "Scene will be divided into segments about this axis.");
        static GUIContent gc_ClusterByLODLevel = new GUIContent("Cluster By LOD Level", "A baker will be created for each LOD level.");
        static GUIContent gc_ClusterDistance = new GUIContent("Max Distance", "Source meshes closer than this value will be grouped into clusters.");
        static GUIContent gc_IncludeCellsWithOnlyOneRenderer = new GUIContent("Include Cells With Only One Renderer", "There is no benefit in combining meshes with only one mesh except to adjust UVs to share an atlas.");
        static GUIContent gc_Settings = new GUIContent("Use Shared Settings Asset", "Different bakers can share the same settings. If this field is None, then the settings below will be used.");
        static GUIContent gc_PieRingSpacing = new GUIContent("Ring Spacing", "Pie segments will be divided into rings.");
        static GUIContent gc_PieCombineAllInCenterRing = new GUIContent("Combine Center Ring Segments Together", "All segments in the centermost ring will be merged into a single segment.");
        static GUIContent gc_ParentSceneObject = new GUIContent("Parent Scene Object","Must be a scene GameObject. Generated combined meshes will be children of this GameObject.");
        static GUIContent gc_prefabOptions_outputFolder = new GUIContent("Prefab Output Folder", "Prefabs will be saved to this output folder.");
        static GUIContent gc_prefabOptions_autoGeneratePrefabs = new GUIContent("Auto Generate Prefabs", "Configure each generated baker to use 'Bake Into Prefab' and generate a prefab in the output folder for the baker.");

        private SerializedObject grouper;
        private SerializedProperty clusterType, gridOrigin, cellSize, clusterOnLMIndex, numSegments, pieAxis, clusterByLODLevel,
            clusterDistance, includeCellsWithOnlyOneRenderer, mbSettings, mbSettingsAsset, pieRingSpacing, pieCombineAllInCenterRing,
            prefabOptions_outputFolder, prefabOptions_autoGeneratePrefabs, parentSceneObject;

        private MB_MeshBakerSettingsEditor meshBakerSettingsMe;
        private MB_MeshBakerSettingsEditor meshBakerSettingsExternal;

        public void OnEnable()
        {
            lastBoundsCheckRefreshTime = 0;
            grouper = new SerializedObject(target);

            SerializedProperty d = grouper.FindProperty("data");

            clusterType = grouper.FindProperty("clusterType");
            includeCellsWithOnlyOneRenderer = d.FindPropertyRelative("includeCellsWithOnlyOneRenderer");
            gridOrigin = d.FindPropertyRelative("origin");
            cellSize = d.FindPropertyRelative("cellSize");
            clusterOnLMIndex = d.FindPropertyRelative("clusterOnLMIndex");
            clusterByLODLevel = d.FindPropertyRelative("clusterByLODLevel");
            numSegments = d.FindPropertyRelative("pieNumSegments");
            pieAxis = d.FindPropertyRelative("pieAxis");
            clusterDistance = d.FindPropertyRelative("maxDistBetweenClusters");
            mbSettings = grouper.FindProperty("meshBakerSettings");
            mbSettingsAsset = grouper.FindProperty("meshBakerSettingsAsset");
            pieRingSpacing = d.FindPropertyRelative("ringSpacing");
            pieCombineAllInCenterRing = d.FindPropertyRelative("combineSegmentsInInnermostRing");

            parentSceneObject = grouper.FindProperty("parentSceneObject");
            prefabOptions_outputFolder = grouper.FindProperty("prefabOptions_outputFolder");
            prefabOptions_autoGeneratePrefabs = grouper.FindProperty("prefabOptions_autoGeneratePrefabs");

            meshBakerSettingsMe = new MB_MeshBakerSettingsEditor();
            meshBakerSettingsMe.OnEnable(mbSettings);
            if (mbSettingsAsset.objectReferenceValue != null)
            {
                meshBakerSettingsExternal = new MB_MeshBakerSettingsEditor();
                UnityEngine.Object targetObj;
                string propertyName;
                ((MB3_MeshCombinerSettings)mbSettingsAsset.objectReferenceValue).GetMeshBakerSettingsAsSerializedProperty(out propertyName, out targetObj);
                SerializedProperty meshBakerSettings = new SerializedObject(targetObj).FindProperty(propertyName);
                meshBakerSettingsExternal.OnEnable(meshBakerSettings);
            }
        }

        public void OnDisable()
        {
            if (meshBakerSettingsMe != null) meshBakerSettingsMe.OnDisable();
            if (meshBakerSettingsExternal != null) meshBakerSettingsExternal.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            grouper.Update();
            DrawGrouperInspector();
            if (GUILayout.Button("Generate Mesh Bakers"))
            {
                for(int tIdx = 0; tIdx < targets.Length; tIdx++)
                {
                    _generateMeshBakers(targets[tIdx]);
                }
            }

            if (GUILayout.Button("Bake All Child MeshBakers"))
            {
                for (int tIdx = 0; tIdx < targets.Length; tIdx++)
                {
                    _bakeAllChildMeshBakers(targets[tIdx], ref grouper);
                }
            }

            string buttonTextEnableRenderers = "Disable Renderers On All Child MeshBaker Source Objects";
            bool enableRenderers = false;
            {
                MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;
                MB3_MeshBakerCommon bc = tbg.GetComponentInChildren<MB3_MeshBakerCommon>();
                if (bc != null && bc.GetObjectsToCombine().Count > 0)
                {
                    GameObject go = bc.GetObjectsToCombine()[0];
                    if (go != null && go.GetComponent<Renderer>() != null && go.GetComponent<Renderer>().enabled == false)
                    {
                        buttonTextEnableRenderers = "Enable Renderers On All Child MeshBaker Source Objects";
                        enableRenderers = true;
                    }
                }
            }

            if (GUILayout.Button(buttonTextEnableRenderers))
            {
                for (int tIdx = 0; tIdx < targets.Length; tIdx++)
                {
                    _enableDisableRenderers(targets[tIdx], enableRenderers);
                }
            }

            if (GUILayout.Button("Delete All Child Mesh Bakers & Combined Meshes"))
            {
                if (EditorUtility.DisplayDialog("Delete Mesh Bakers", "Delete All Child Mesh Bakers?", "OK", "Cancel"))
                {
                    for (int i = 0; i < targets.Length; i++)
                    {
                        MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)targets[i];
                        tbg.DeleteAllChildMeshBakers();
                    }
                }
            }


            if (DateTime.UtcNow.Ticks - lastBoundsCheckRefreshTime > 10000000)
            {
                MB3_TextureBaker tb = ((MB3_MeshBakerGrouper)target).GetComponent<MB3_TextureBaker>();
                if (tb != null)
                {
                    MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;
                    List<GameObject> gos = tb.GetObjectsToCombine();
                    Bounds b = new Bounds(Vector3.zero, Vector3.one);
                    if (gos.Count > 0 && gos[0] != null && gos[0].GetComponent<Renderer>() != null)
                    {
                        b = gos[0].GetComponent<Renderer>().bounds;
                    }
                    for (int i = 0; i < gos.Count; i++)
                    {
                        if (gos[i] != null && gos[i].GetComponent<Renderer>() != null)
                        {
                            b.Encapsulate(gos[i].GetComponent<Renderer>().bounds);
                        }
                    }

                    tbg.sourceObjectBounds = b;
                    lastBoundsCheckRefreshTime = DateTime.UtcNow.Ticks;
                }
            }

            grouper.ApplyModifiedProperties();
        }

        public void DrawGrouperInspector()
        {
            EditorGUILayout.HelpBox("This component groups meshes that are close together so they can be combined." +
                                " It generates multiple MB3_MeshBaker objects from the list of Objects To Be Combined in the MB3_TextureBaker component." +
                                " Objects that are close together will be grouped together and added to a new child MB3_MeshBaker object.\n\n" +
                                " TIP: Try the new agglomerative cluster type!", MessageType.Info);
            MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;

            MB3_TextureBaker tb = tbg.GetComponent<MB3_TextureBaker>();

            Transform pgo = (Transform)EditorGUILayout.ObjectField(gc_ParentSceneObject, parentSceneObject.objectReferenceValue, typeof(Transform), true);
            if (pgo != null && MB_Utility.IsSceneInstance(pgo.gameObject))
            {
                parentSceneObject.objectReferenceValue = pgo;
            }
            else
            {
                parentSceneObject.objectReferenceValue = null;
            }

            EditorGUILayout.PropertyField(clusterType, gc_ClusterType);
            // Confusion warning (don't use clusterType.enumValueIndex. It is the index in the list of display names. NOT the enum value)
            MB3_MeshBakerGrouper.ClusterType gg = (MB3_MeshBakerGrouper.ClusterType)clusterType.intValue;
            if ((gg == MB3_MeshBakerGrouper.ClusterType.none && !(tbg.grouper is MB3_MeshBakerGrouperNone)) ||
                (gg == MB3_MeshBakerGrouper.ClusterType.grid && !(tbg.grouper is MB3_MeshBakerGrouperGrid)) ||
                (gg == MB3_MeshBakerGrouper.ClusterType.pie && !(tbg.grouper is MB3_MeshBakerGrouperPie)) ||
                (gg == MB3_MeshBakerGrouper.ClusterType.agglomerative && !(tbg.grouper is MB3_MeshBakerGrouperCluster))
                )
            {
                tbg.CreateGrouper(gg, tbg.data);
                tbg.clusterType = gg;
            }

			// Confusion warning (don't use clusterType.enumValueIndex. It is the index in the list of display names. NOT the enum value)
            if (clusterType.intValue == (int)MB3_MeshBakerGrouper.ClusterType.grid)
            {
                EditorGUILayout.PropertyField(gridOrigin, gc_GridOrigin);
                EditorGUILayout.PropertyField(cellSize, gc_CellSize);
            }
            else if (clusterType.intValue == (int)MB3_MeshBakerGrouper.ClusterType.pie)
            {
                EditorGUILayout.PropertyField(gridOrigin, gc_GridOrigin);
                EditorGUILayout.PropertyField(numSegments, gc_NumSegements);
                EditorGUILayout.PropertyField(pieAxis, gc_PieAxis);
                EditorGUILayout.PropertyField(pieRingSpacing, gc_PieRingSpacing);
                EditorGUILayout.PropertyField(pieCombineAllInCenterRing, gc_PieCombineAllInCenterRing);
            }
            else if (clusterType.intValue == (int)MB3_MeshBakerGrouper.ClusterType.agglomerative)
            {
                float dist = clusterDistance.floatValue;
                float maxDist = 100f;
                float minDist = .000001f;
                MB3_MeshBakerGrouperCluster cl = null;
                if (tbg.grouper is MB3_MeshBakerGrouperCluster)
                {
                    cl = (MB3_MeshBakerGrouperCluster)tbg.grouper;
                    maxDist = cl._ObjsExtents;
                    minDist = cl._minDistBetweenClusters;
                    if (dist < minDist)
                    {
                        dist = Mathf.Lerp(minDist, maxDist, .11f);
                    }
                }

                dist = EditorGUILayout.Slider(gc_ClusterDistance, dist, minDist, maxDist);
                clusterDistance.floatValue = dist;

                string btnName = "Refresh Clusters";
                if (cl.cluster == null || cl.cluster.clusters == null || cl.cluster.clusters.Length == 0)
                {
                    btnName = "Click To Build Clusters";
                }
                if (GUILayout.Button(btnName))
                {
                    if (tbg.grouper is MB3_MeshBakerGrouperCluster)
                    {
                        MB3_MeshBakerGrouperCluster cg = (MB3_MeshBakerGrouperCluster)tbg.grouper;
                        if (tb != null)
                        {
                            cg.BuildClusters(tb.GetObjectsToCombine(), updateProgressBar);
                            EditorUtility.ClearProgressBar();
                            Repaint();
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(clusterOnLMIndex, gc_ClusterOnLMIndex);
            EditorGUILayout.PropertyField(clusterByLODLevel, gc_ClusterByLODLevel);
            EditorGUILayout.PropertyField(includeCellsWithOnlyOneRenderer, gc_IncludeCellsWithOnlyOneRenderer);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab Output Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prefabOptions_autoGeneratePrefabs, gc_prefabOptions_autoGeneratePrefabs);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prefabOptions_outputFolder, gc_prefabOptions_outputFolder);
            if (GUILayout.Button("Browse"))
            {
                string path = EditorUtility.OpenFolderPanel("Browse For Output Folder", "", "");
                path = MB_BatchPrefabBakerEditorFunctions.ConvertFullPathToProjectRelativePath(path);
                prefabOptions_outputFolder.stringValue = path;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Baker Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These settings will be shared by all created child MeshBaker components.", MessageType.Info);

            UnityEngine.Object oldObjVal = mbSettingsAsset.objectReferenceValue;
            EditorGUILayout.PropertyField(mbSettingsAsset, gc_Settings);

            bool doingTextureArrays = false;
            if (tb != null && tb.textureBakeResults != null) doingTextureArrays = tb.textureBakeResults.resultType == MB2_TextureBakeResults.ResultType.textureArray;
            if (mbSettingsAsset.objectReferenceValue == null)
            {
                meshBakerSettingsMe.DrawGUI(tbg.meshBakerSettings, true, doingTextureArrays);
            }
            else
            {
                if (meshBakerSettingsExternal == null || oldObjVal != mbSettingsAsset.objectReferenceValue)
                {
                    if (meshBakerSettingsExternal == null) meshBakerSettingsExternal = new MB_MeshBakerSettingsEditor();
                    UnityEngine.Object targetObj;
                    string propertyName;
                    ((MB3_MeshCombinerSettings)mbSettingsAsset.objectReferenceValue).GetMeshBakerSettingsAsSerializedProperty(out propertyName, out targetObj);
                    SerializedProperty meshBakerSettings = new SerializedObject(targetObj).FindProperty(propertyName);
                    meshBakerSettingsExternal.OnEnable(meshBakerSettings);
                }

                meshBakerSettingsExternal.DrawGUI(((MB3_MeshCombinerSettings)mbSettingsAsset.objectReferenceValue).data, false, doingTextureArrays);
            }
        }

        private static void _generateMeshBakers(UnityEngine.Object target)
        {
            MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;
            MB3_TextureBaker tb = tbg.GetComponent<MB3_TextureBaker>();
            if (tb == null)
            {
                Debug.LogError("There must be an MB3_TextureBaker attached to this game object.");
                return;
            }

            if (tb.GetObjectsToCombine().Count == 0)
            {
                Debug.LogError("The MB3_MeshBakerGrouper creates clusters based on the objects to combine in the MB3_TextureBaker component. There were no objects in this list.");
                return;
            }

            if (tbg.parentSceneObject == null ||
                !MB_Utility.IsSceneInstance(tbg.parentSceneObject.gameObject))
            {
                GameObject g = new GameObject("CombinedMeshes-" + tbg.name);
                tbg.parentSceneObject = g.transform;
            }

            //check if any of the objes that will be added to bakers already exist in child bakers
            List<GameObject> objsWeAreGrouping = tb.GetObjectsToCombine();
            MB3_MeshBakerCommon[] alreadyExistBakers = tbg.GetComponentsInChildren<MB3_MeshBakerCommon>();
            bool foundChildBakersWithObjsToCombine = false;
            for (int i = 0; i < alreadyExistBakers.Length; i++)
            {
                List<GameObject> childOjs2Combine = alreadyExistBakers[i].GetObjectsToCombine();
                for (int j = 0; j < childOjs2Combine.Count; j++)
                {
                    if (childOjs2Combine[j] != null && objsWeAreGrouping.Contains(childOjs2Combine[j]))
                    {
                        foundChildBakersWithObjsToCombine = true;
                        break;
                    }
                }
            }

            bool proceed = true;
            if (foundChildBakersWithObjsToCombine)
            {
                proceed = EditorUtility.DisplayDialog("Replace Previous Generated MeshBaker Objects", "Delete child MeshBaker objects?\n\n" +
                    "This grouper has child MeshBaker objects from a previous clustering. Do you want to delete these and create new ones?", "OK", "Cancel");
            }

            if (tbg.prefabOptions_autoGeneratePrefabs)
            {
                if (!MB_BatchPrefabBakerEditorFunctions.ValidateFolderIsInProject("Output Folder", tbg.prefabOptions_outputFolder))
                {
                    Debug.LogError("If " + gc_prefabOptions_autoGeneratePrefabs.text + " is enabled, you must provide an output folder. Prefabs will be saved in this folder.");
                    proceed = false;
                }
            }

            if (proceed)
            {
                if (foundChildBakersWithObjsToCombine) tbg.DeleteAllChildMeshBakers();
                List<MB3_MeshBakerCommon> newBakers = tbg.grouper.DoClustering(tb, tbg);
                if (newBakers.Count > 0) DoGeneratePrefabsIfNecessary(tbg, newBakers);
            }
        }

        private static void DoGeneratePrefabsIfNecessary(MB3_MeshBakerGrouper grouper, List<MB3_MeshBakerCommon> newBakers)
        {
            if (!grouper.prefabOptions_autoGeneratePrefabs &&
                !grouper.prefabOptions_mergeOutputIntoSinglePrefab) return;
            if (!MB_BatchPrefabBakerEditorFunctions.ValidateFolderIsInProject("Output Folder", grouper.prefabOptions_outputFolder)) return;

            if (grouper.prefabOptions_autoGeneratePrefabs)
            {
                for (int i = 0; i < newBakers.Count; i++)
                {
                    MB3_MeshBakerCommon baker = newBakers[i];

                    string path = grouper.prefabOptions_outputFolder;
                    // To handle paths with a different root
                    //path = MB_BatchPrefabBakerEditorFunctions.ConvertAnyPathToProjectRelativePath(path);
                    
                    // Generate a new prefab name
                    string prefabName = baker.name.Replace("MeshBaker", "CombinedMesh");
                    prefabName = prefabName.Replace(" ", "_");
                    prefabName = prefabName.Replace(",", "_");
                    prefabName = prefabName.Trim(Path.GetInvalidFileNameChars());
                    prefabName = prefabName.Trim(Path.GetInvalidPathChars());

                    string pathName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + prefabName + ".prefab");
                    if (pathName == null || pathName.Length == 0)
                    {
                        Debug.LogError("Could not generate prefab " + prefabName + " in folder " + path + ". There is something wrong with the path or prefab name.");
                        continue;
                    }

                    // Generate a new prefab
                    GameObject go = new GameObject(baker.name);
                    GameObject pf = MBVersionEditor.PrefabUtility_CreatePrefab(pathName, go);

                    // Configure the baker to bake into the prefab
                    baker.resultPrefab = pf;
                    baker.resultPrefabLeaveInstanceInSceneAfterBake = true;
                    baker.meshCombiner.outputOption = MB2_OutputOptions.bakeIntoPrefab;
                    if (grouper.parentSceneObject != null)
                    {
                        baker.parentSceneObject = grouper.parentSceneObject;
                    }

                    MB_Utility.Destroy(go);
                }
            }
        }

        private static void _bakeAllChildMeshBakers(UnityEngine.Object target, ref SerializedObject grouper)
        {
            MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;
            try
            {
                MB3_MeshBakerCommon[] mBakers = tbg.GetComponentsInChildren<MB3_MeshBakerCommon>();
                for (int i = 0; i < mBakers.Length; i++)
                {
                    bool createdDummyMaterialBakeResult;
                    if (grouper.targetObject == tbg)
                    {
                        MB3_MeshBakerEditorFunctions.BakeIntoCombined(mBakers[i], out createdDummyMaterialBakeResult, ref grouper);
                    }
                    else
                    {
                        MB3_MeshBakerEditorFunctions.BakeIntoCombined(mBakers[i], out createdDummyMaterialBakeResult);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void _enableDisableRenderers(UnityEngine.Object target, bool enableRenderers)
        {
            MB3_MeshBakerGrouper tbg = (MB3_MeshBakerGrouper)target;
            try
            {
                MB3_MeshBakerCommon[] mBakers = tbg.GetComponentsInChildren<MB3_MeshBakerCommon>();
                for (int i = 0; i < mBakers.Length; i++)
                {
                    mBakers[i].EnableDisableSourceObjectRenderers(enableRenderers);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message + "\n" + ex.StackTrace.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public bool updateProgressBar(string msg, float progress)
        {
            //EditorUtility.DisplayProgressBar("Creating Clusters", msg, progress);
            return EditorUtility.DisplayCancelableProgressBar("Creating Clusters", msg, progress);
        }
    }
}
