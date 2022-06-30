using UnityEngine;
using System.Collections;

public class MB_PrepareObjectsForDynamicBatchingDescription : MonoBehaviour {
	
	void OnGUI(){
		GUILayout.Label ("This scene creates a combined material and meshes with adjusted UVs so objects \n" +
						 " can share a material and be batched by Unity's static/dynamic batching.\n" + 
						 " Output has been set to 'bakeMeshAssetsInPlace' on the Mesh Baker\n" +
						 " Position, Scale and Rotation will be baked into meshes so place them appropriately.\n" + 
						 " Dynamic batching requires objects with uniform scale. You can fix non-uniform scale here\n" + 
						 " After baking you need to duplicate your source prefab assets and replace the  \n" +
						 " meshes and materials with the generated ones.\n");				
	}
}
