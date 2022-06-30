using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class DestroyIfPlayMode : MonoBehaviour {
        // Start is called before the first frame update
        void Start() {
            Debug.Log("Should not exist in Play Mode. Destroying GameObject");
            GameObject.Destroy(this.gameObject);
        }
    }
}

