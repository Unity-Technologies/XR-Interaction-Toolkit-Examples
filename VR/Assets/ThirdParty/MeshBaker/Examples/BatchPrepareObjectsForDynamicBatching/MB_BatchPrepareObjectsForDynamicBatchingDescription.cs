using UnityEngine;
using System.Collections;

public class MB_BatchPrepareObjectsForDynamicBatchingDescription : MonoBehaviour {
	
	void OnGUI(){
		GUILayout.Label ("This scene is set up to create a combined material and meshes with adjusted UVs so \n" +
						 " objects can share a material and be batched by Unity's static/dynamic batching.\n" + 
						 " This scene has added a BatchPrefabBaker component to a Mesh and Material Baker which \n" +
						 "  can bake many prefabs (each of which can have several renderers) in one click.\n" + 
						 " The batching tool accepts prefab assets instead of scene objects. \n");				
	}
}
