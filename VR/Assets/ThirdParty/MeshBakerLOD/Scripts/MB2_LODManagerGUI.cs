using UnityEngine;
using System.Collections;

public class MB2_LODManagerGUI : MonoBehaviour {
	string text;
	
	void OnGUI () {
		MB2_LODManager m = GetComponent<MB2_LODManager>();
		if (GUI.Button(new Rect(0,0,100,20),"LOD Stats")){
			if (m != null)
				text = m.GetStats();
			else 
				text = "Could not find LODManager";
			Debug.Log(text);
		}
		GUI.Label(new Rect(0,20,300,600), text);
	}
}
