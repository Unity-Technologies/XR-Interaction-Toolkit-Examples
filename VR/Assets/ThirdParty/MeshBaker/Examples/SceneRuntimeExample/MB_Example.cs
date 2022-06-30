using UnityEngine;
using System.Collections;

public class MB_Example : MonoBehaviour {
	
	public MB3_MeshBaker meshbaker;
	public GameObject[] objsToCombine;
	
   	void Start(){
	  //Add the objects to the combined mesh	
	  //Must have previously baked textures for these in the editor
      meshbaker.AddDeleteGameObjects(objsToCombine, null, true);
      //apply the changes we made this can be slow. See documentation
	  meshbaker.Apply();
	}
	
	void LateUpdate(){
		//Apply changes after this and other scripts have made changes
		//Only to vertecies, tangents and normals
		//Only want to call this once per frame since it is slow
		meshbaker.UpdateGameObjects(objsToCombine);
		meshbaker.Apply(false,true,true,true,false,false,false,false,false);	
	}
	
	void OnGUI(){
		GUILayout.Label ("Dynamically updates the vertices, normals and tangents in combined mesh every frame.\n" +
						 "This is similar to dynamic batching. It is not recommended to do this every frame.\n" +
						 "Also consider baking the mesh renderer objects into a skinned mesh renderer\n" +
						 "The skinned mesh approach is faster for objects that need to move independently of each other every frame.");				
	}
}
