using UnityEngine;
using System.Collections;

public class MB_ExampleMover : MonoBehaviour {
	
	public int axis = 0;
	
	void Update () {
		Vector3 v1 = new Vector3(5f,5f,5f);
		v1[axis] *= Mathf.Sin(Time.time);
		transform.position = v1;
	}
}
