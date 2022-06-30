using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MB3_DisableHiddenAnimations : MonoBehaviour {
	public List<Animation> animationsToCull = new List<Animation>();
	
	void Start () {
		if (GetComponent<SkinnedMeshRenderer> () == null) {
			Debug.LogError ("The MB3_CullHiddenAnimations script was placed on and object " + name + " which has no SkinnedMeshRenderer attached");
		}
	}

	void OnBecameVisible(){
		for (int i = 0; i < animationsToCull.Count; i++) {
			if (animationsToCull[i] != null) animationsToCull[i].enabled = true;
		}
	}

	void OnBecameInvisible(){
		for (int i = 0; i < animationsToCull.Count; i++) {
			if (animationsToCull[i] != null) animationsToCull[i].enabled = false;
		}
	}
}
