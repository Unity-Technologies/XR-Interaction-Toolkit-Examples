using UnityEngine;
using System.Collections;

public class MB_ExampleSkinnedMeshDescription : MonoBehaviour {
	
	void OnGUI(){
		GUILayout.Label ("Mesh Renderer objects have been baked into a skinned mesh. Each source object\n" +
						 " is still in the scene (with renderer disabled) and becomes a bone. Any scripts, animations,\n" + 
						 " or physics that affect the invisible source objects will be visible in the\n" +
						 "Skinned Mesh." + 
						 " This approach is more efficient than either dynamic batching or updating every frame \n" + 
						 " for many small objects that constantly and independently move. \n" +
						 " With this approach pay attention to the SkinnedMeshRenderer Bounds and Animation Culling\n" +
						 "settings. You may need to write your own script to manage/update these or your object may vanish or stop animating.\n" +
						 " You can update the combined mesh at runtime as objects are added and deleted from the scene.");				
	}
}
