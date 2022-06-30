using UnityEngine;
using System.Collections;

public class TestForceBakeAndUpdate : MonoBehaviour {
	
	public Transform player;
	
	// Update is called once per frame
	void Update () {
		if (Time.frameCount == 500){
			float t = Time.timeSinceLevelLoad;
			Debug.Log ("Moving player");
			player.position += new Vector3(0f,0f,250f);
			Camera.main.transform.position = player.position + Vector3.forward * 10f;
			Debug.Log ("Forcing check");
			MB2_LODManager.Manager().checkScheduler.ForceCheckIfLODsNeedToChange();
			Debug.Log ("Forcing bake");
			MB2_LODManager.Manager().ForceBakeAllDirty();
			Debug.LogError ("Done, took " + (Time.timeSinceLevelLoad - t).ToString("F8"));
		}
	}
}
