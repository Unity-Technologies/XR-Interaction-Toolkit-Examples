using UnityEngine;
using System.Collections;

[RequireComponent (typeof (Camera))]
[AddComponentMenu("Mesh Baker/LOD Camera")]
public class MB2_LODCamera : MonoBehaviour {
	void Awake(){
		MB2_LODManager m = MB2_LODManager.Manager();
		if (m != null) m.AddCamera(this);
	}
	
	void OnDestroy(){
		MB2_LODManager m = MB2_LODManager.Manager();
		if (m != null) m.RemoveCamera(this);	
	} 
}
