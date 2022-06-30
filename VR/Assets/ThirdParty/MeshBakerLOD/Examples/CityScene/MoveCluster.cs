using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Lod;

/*

If your world is large it can become necessary to move all meshes so that the camera is close
to the origin. This script shows an example of shifting the entire world.

*/

public class MoveCluster : MonoBehaviour {
	public Transform world;
	
	// Translating the world should happen in LateUpdate which is after all LODs have been checked this frame.
	void LateUpdate () {
		if (Time.frameCount % 300 == 0){
			MB2_LODManager manager = (MB2_LODManager) FindObjectOfType(typeof(MB2_LODManager));
			Vector3 newPosition = new Vector3(Random.Range(-1000f,1000f),Random.Range(-1000f,1000f),Random.Range(-1000f,1000f));
			Vector3 translation = newPosition - world.position;
			world.position += translation;
			//The LOD objects, camera, player etc... need to be moved before the call to TranslateAllClusters
			manager.TranslateWorld(translation);
			Debug.Log("Moving World To " + newPosition);
		}
	}
}
			  