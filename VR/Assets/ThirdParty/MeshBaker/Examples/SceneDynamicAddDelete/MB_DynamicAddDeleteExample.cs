using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MB_DynamicAddDeleteExample : MonoBehaviour {
	public GameObject prefab;
	List<GameObject> objsInCombined = new List<GameObject>();
	MB3_MultiMeshBaker mbd;
	GameObject[] objs;

	float GaussianValue(){
		float x1, x2, w, y1;
		
		do {
			x1 = 2.0f * Random.Range(0f,1f) - 1.0f;
			x2 = 2.0f * Random.Range(0f,1f) - 1.0f;
			w = x1 * x1 + x2 * x2;
		} while ( w >= 1.0f );
		
		w = Mathf.Sqrt( (-2.0f * Mathf.Log( w ) ) / w );
		y1 = x1 * w;
		return y1;
	}

	void Start(){
		mbd = GetComponentInChildren<MB3_MultiMeshBaker>(); 
		
		// instantiate game objects
		int dim = 10;
		GameObject[] gos = new GameObject[dim * dim];
		for (int i = 0; i < dim; i++){
			for (int j = 0; j < dim; j++){
				GameObject go = (GameObject) Instantiate(prefab);
				gos[i*dim + j] = go.GetComponentInChildren<MeshRenderer>().gameObject;
				float randx = Random.Range(-4f,4f);
				float randz = Random.Range(-4f,4f);
				go.transform.position = (new Vector3(3f*i + randx, 0, 3f * j + randz));
				float randrot = Random.Range (0,360);
				go.transform.rotation = Quaternion.Euler(0,randrot,0);
				Vector3 randscale = Vector3.one + Vector3.one * GaussianValue() * .15f;
				go.transform.localScale = randscale;
				//put every third object in a list so we can add and delete it later
				if ((i*dim + j) % 3 == 0){
					objsInCombined.Add(gos[i*dim + j]);
				}
			}
		}
		//add objects to combined mesh
		mbd.AddDeleteGameObjects(gos, null, true);
		mbd.Apply();
		
		objs = objsInCombined.ToArray();
		//start routine which will periodically add and delete objects
		StartCoroutine(largeNumber());
	}
	
	IEnumerator largeNumber() {
		while(true){
			yield return new WaitForSeconds(1.5f);
			//Delete every third object
			mbd.AddDeleteGameObjects(null, objs, true);
			mbd.Apply();
			
			yield return new WaitForSeconds(1.5f);
			//Add objects back
			mbd.AddDeleteGameObjects(objs, null, true);
			mbd.Apply();
		}
	}
	
	void OnGUI(){
		GUILayout.Label ("Dynamically instantiates game objects. \nRepeatedly adds and removes some of them\n from the combined mesh.");	
	}
}
