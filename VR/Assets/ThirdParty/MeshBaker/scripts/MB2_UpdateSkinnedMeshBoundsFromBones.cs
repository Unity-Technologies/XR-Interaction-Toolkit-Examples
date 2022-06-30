using UnityEngine;
using System.Collections;
using DigitalOpus.MB.Core;

public class MB2_UpdateSkinnedMeshBoundsFromBones : MonoBehaviour {
    SkinnedMeshRenderer smr;
	Transform[] bones;
     
	void Start () {
			smr = GetComponent<SkinnedMeshRenderer>();
			if (smr == null){
				Debug.LogError("Need to attach MB2_UpdateSkinnedMeshBoundsFromBones script to an object with a SkinnedMeshRenderer component attached.");
				return;
			}
			bones = smr.bones;
            bool origVal = smr.updateWhenOffscreen;
			smr.updateWhenOffscreen = true;
			smr.updateWhenOffscreen = origVal;
        }
    
	void Update () {
        if (smr != null){
			MB3_MeshCombiner.UpdateSkinnedMeshApproximateBoundsFromBonesStatic(bones,smr);
		}
	}
}
