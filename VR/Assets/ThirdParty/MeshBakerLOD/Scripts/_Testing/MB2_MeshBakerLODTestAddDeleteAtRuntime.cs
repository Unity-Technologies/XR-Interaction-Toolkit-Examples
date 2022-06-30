using UnityEngine;
using System.Collections;
using DigitalOpus.MB.Lod;
using DigitalOpus.MB.Core;

public class MB2_MeshBakerLODTestAddDeleteAtRuntime : MonoBehaviour {
	public MB3_MeshBaker mb1;
	public MB3_MeshBaker mb2;

	public GameObject treePrefab;
	
	MB2_LODManager.BakerPrototype bp= new MB2_LODManager.BakerPrototype();
	// Update is called once per frame
	void Update () {
		Debug.Log("Adding baker");
		if (Time.frameCount == 100){
			bp.meshBaker = mb1;
			MB2_LODManager.Manager().AddBaker(bp);
		}
		Debug.Log("Instantiate prefab");
		if (Time.frameCount == 200){
			Debug.Log("b " + (bp == MB2_LODManager.Manager().bakers[0]));
			GameObject tree = (GameObject) Instantiate(treePrefab);
			tree.transform.Translate(new Vector3(10f,0,0));
			Debug.Log("c " + (bp == MB2_LODManager.Manager().bakers[0]));
		}
		Debug.Log("Instantiate prefab");
		if (Time.frameCount == 300){
			GameObject tree = (GameObject) Instantiate(treePrefab);
			tree.transform.Translate(new Vector3(10f,0,0));
			Debug.Log("a " + (bp == MB2_LODManager.Manager().bakers[0]));
		}
		
		Debug.Log("Remove baker");
		if (Time.frameCount == 400){
			Debug.Log((bp == MB2_LODManager.Manager().bakers[0]));
			MB2_LODManager.Manager().RemoveBaker(bp);
		}
	}
}
