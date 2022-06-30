using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MB2_LODSoloChecker : MonoBehaviour {

    MB2_LOD lod;

    private void Start()
    {
        lod = GetComponent<MB2_LOD>();
    }
	
	// Update is called once per frame
	void Update () {
        if (lod == null) return;
        lod.CheckIfLODsNeedToChange();
	}
}
