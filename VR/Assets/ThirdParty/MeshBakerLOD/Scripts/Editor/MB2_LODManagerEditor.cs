using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

using UnityEditor;

[CustomEditor(typeof(MB2_LODManager))]
public class MB2_LODManagerEditor : Editor {
	
	private static GUIContent
		insertBakerContent = new GUIContent("+", "add baker"),
		deleteBakerContent = new GUIContent("-", "delete this baker"),
		numFramesBeforeGarbageCollectContent = new GUIContent("Num Bakes Between GC Calls", "Combining meshes requires a lot of array allocations. Discarded arrays can accumulate in memory until memory gets low and garbage collection is triggered. This can cause a pause in the framerate. Setting this value to something greater than one will cause the garbage collector to be called regularly which will cause many small pauses instead of one large one.\n\n" + 
			"It is best to see how long garbage collection is taking in the MB2_LODManager stats field of the MB2_LODManager and set this value accordingly.\n\n" +
			"Set this value to -1 to disable garbage collection calls by the MB2_LODManager."),
		maxCombineTimePerFrameContent = new GUIContent("Combine Time Per Frame (s)", "MeshBaker LOD will try to distribute bakes across several frames to avoid stuttering. The LOD Manager will continue baking until this threshold is exceeded. If there is a lot of baking to do then the load will be distributed across several frames. A resonable setting would be slightly less than 1 / desiredFrameRate in seconds."),
		
		meshBakerContent = new GUIContent("Mesh Baker", "A Configured MB2_MeshBaker component. This baker will be used as a prototype for the combined LOD meshes. This component is not used directly but its settings are cloned for the combined meshes."),
		clusterTypeContent = new GUIContent("Cluster Type", "The clustering scheme to use. \n\n" +
				"SIMPLE: All meshes are combined into one big combined mesh. Skinned meshes must use this option\n\n" +
				"GRID: Space is divided into cubic volumes and all meshes in each volume are combined.\n"),
		gridSizeContent = new GUIContent("Grid Size", "The size of the grid cluster cells"),
		numFramesBetweenChecksContent = new GUIContent("Num Frames Between Checks", ""),
		labelContent = new GUIContent("Label", "Optional. Sometimes LOD objects could be baked by multiple bakers. To force an LOD component to be baked by this baker, specify a label here and enter it in the LOD's label field."),
		lightmapIndexContent = new GUIContent("Lightmap Index", ""),
		layerContent = new GUIContent("Layer", "Layer for combined mesh."),
		maxVertsPerCombinedContent = new GUIContent("Max Vertices Per Combined Mesh", "The maximum number of vertices per combined mesh. Even though meshes can contain up to 64k vertices you may not want to use meshes that big. Large meshes take a long time to update. For meshes that are updated frequently it is better to use smaller meshes."),
		//maxNumberPerLevelContent = new GUIContent("Max Number Per Level", "Optional array specifying the maximum number of meshes allowed in each level of detail. Useful for mobs to ensure that only a fixed number of the closest meshes are at the highest level of detail."),
		updateSkinnedMeshBoundsContent = new GUIContent("Update Skinned Mesh Bounds", "Unity culls skinned meshes based on bounds (which is not updated). If you are combining skinned meshes that can wander outside the fixed bounds then you should set this option or your meshes might vanish unexpectedly.");

	private static GUILayoutOption
		buttonWidth = GUILayout.MaxWidth(20f),
		labelWidth = GUILayout.MaxWidth(440f);	
	
	private SerializedObject manager;
	private SerializedProperty bakers, logLevel, numBakersPerGC, maxCombineTimePerFrame, ignoreLightmapping;	
	
	public void OnEnable (){
		manager = new SerializedObject(target);
		bakers = manager.FindProperty("bakers");
		logLevel = manager.FindProperty("LOG_LEVEL");
		numBakersPerGC = manager.FindProperty("numBakersPerGC");
		maxCombineTimePerFrame = manager.FindProperty("maxCombineTimePerFrame");
		ignoreLightmapping = manager.FindProperty("ignoreLightmapping");
	}
	
	public override void OnInspectorGUI(){
		manager.Update();
		MB2_LODManager m = (MB2_LODManager) target;
		MBVersionEditor.SetInspectorLabelWidth(200f);
		EditorGUILayout.PropertyField(logLevel);
		EditorGUILayout.PropertyField(numBakersPerGC, numFramesBeforeGarbageCollectContent);
		EditorGUILayout.PropertyField(maxCombineTimePerFrame, maxCombineTimePerFrameContent,labelWidth);
		EditorGUILayout.PropertyField(ignoreLightmapping);
		GUILayout.Label("Bakers",EditorStyles.boldLabel);
		
		if (bakers.arraySize == 0){
			if (GUILayout.Button("Add New Baker")){
				m.bakers = new MB2_LODManager.BakerPrototype[1];
				m.bakers[0] = new MB2_LODManager.BakerPrototype();
			}
		}
		
		for(int i = 0; i < bakers.arraySize; i++){
			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal(); 
			GUILayout.Label("--Baker " + i,EditorStyles.boldLabel);
			if(GUILayout.Button(insertBakerContent, EditorStyles.miniButtonMid, buttonWidth)){
				bakers.InsertArrayElementAtIndex(i);
			}
			if(GUILayout.Button(deleteBakerContent, EditorStyles.miniButtonRight, buttonWidth)){
				bakers.DeleteArrayElementAtIndex(i);
				continue;
			}			
			EditorGUILayout.EndHorizontal();
			SerializedProperty baker = bakers.GetArrayElementAtIndex(i);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("meshBaker"), meshBakerContent);
			SerializedProperty clusterType = baker.FindPropertyRelative("clusterType");
			EditorGUILayout.PropertyField(clusterType,clusterTypeContent);
			if (clusterType.intValue != (int) MB2_LODManager.BakerPrototype.CombinerType.simple){
				EditorGUILayout.PropertyField(baker.FindPropertyRelative("gridSize"), gridSizeContent);
			}
			
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("numFramesBetweenLODChecks"),numFramesBetweenChecksContent);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("label"),labelContent);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("lightMapIndex"),lightmapIndexContent);
			SerializedProperty layer = baker.FindPropertyRelative("layer");
			layer.intValue = EditorGUILayout.LayerField(layerContent,layer.intValue);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("castShadow"));
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("receiveShadow"));
			
			//EditorGUILayout.PropertyField(baker.FindPropertyRelative("layer"),layerContent);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("maxVerticesPerCombinedMesh"),maxVertsPerCombinedContent);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("updateSkinnedMeshApproximateBounds"), updateSkinnedMeshBoundsContent);
			EditorGUILayout.PropertyField(baker.FindPropertyRelative("maxNumberPerLevel"),true);
		}		
		
		
		GUILayout.Label("Statistics",EditorStyles.boldLabel);
		EditorGUILayout.HelpBox(m.GetStats(),MessageType.Info);
		manager.ApplyModifiedProperties();
	}
	
	void setupBakers(){
		MB2_LODManager manager = (MB2_LODManager) target;
		MB3_MeshBaker[] tbs = manager.GetComponentsInChildren<MB3_MeshBaker>();
		
		List<MB2_LODManager.BakerPrototype> newBakers = new List<MB2_LODManager.BakerPrototype>();
		for (int i = 0; i < tbs.Length; i++){
			MB2_LODManager.BakerPrototype p = null;
			for (int j = 0; j < manager.bakers.Length; j++){
				if (manager.bakers[j].meshBaker == tbs[i]){
					p = manager.bakers[j];
				}
			}
			if (p == null){
				p = new MB2_LODManager.BakerPrototype();
				newBakers.Add(p);
			}
			p.meshBaker = tbs[i];
			
			//todo get lightmap index from textureBakeResults
			GameObject go = tbs[i].GetObjectsToCombine()[0];
			Renderer r = MB_Utility.GetRenderer(go);
			p.lightMapIndex = r.lightmapIndex;
		}
		
		MB2_LODManager.BakerPrototype[] ps = new MB2_LODManager.BakerPrototype[manager.bakers.Length + newBakers.Count];
		Array.Copy(manager.bakers, ps, 0);
		Array.Copy(newBakers.ToArray(), 0, ps, manager.bakers.Length, newBakers.Count);
		manager.bakers = ps;
	}
}
