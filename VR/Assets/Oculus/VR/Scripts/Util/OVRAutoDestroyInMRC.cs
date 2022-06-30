using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// If there is a game object under the main camera which should not be cloned under Mixed Reality Capture,
// attaching this component would auto destroy that after the MRC camera get cloned
public class OVRAutoDestroyInMRC : MonoBehaviour {

	// Use this for initialization
	void Start () {
		bool underMrcCamera = false;

		Transform p = transform.parent;
		while (p != null)
		{
			if (p.gameObject.name.StartsWith("OculusMRC_"))
			{
				underMrcCamera = true;
				break;
			}
			p = p.parent;
		}

		if (underMrcCamera)
		{
			Destroy(gameObject);
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
