using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

public class MB2_UpdateSkinnedMeshBoundsFromBounds : MonoBehaviour {
	public List<GameObject> objects;
    SkinnedMeshRenderer smr;
     
	void Start () {
			smr = GetComponent<SkinnedMeshRenderer>();
			if (smr == null){
				Debug.LogError("Need to attach MB2_UpdateSkinnedMeshBoundsFromBounds script to an object with a SkinnedMeshRenderer component attached.");
				return;
			}
			if (objects == null || objects.Count == 0){
				Debug.LogWarning("The MB2_UpdateSkinnedMeshBoundsFromBounds had no Game Objects. It should have the same list of game objects that the MeshBaker does.");
				smr = null;
				return;
			}
			for (int i = 0; i < objects.Count; i++){
				if (objects[i] == null || objects[i].GetComponent<Renderer>() == null){
					Debug.LogError("The list of objects had nulls or game objects without a renderer attached at position " + i);
					smr = null;
					return;
				}
			}
            bool origVal = smr.updateWhenOffscreen;
			smr.updateWhenOffscreen = true;
			smr.updateWhenOffscreen = origVal;
        }
    
	void Update () {
        if (smr != null && objects != null){
			MB3_MeshCombiner.UpdateSkinnedMeshApproximateBoundsFromBoundsStatic(objects,smr);
		}
	}
}
