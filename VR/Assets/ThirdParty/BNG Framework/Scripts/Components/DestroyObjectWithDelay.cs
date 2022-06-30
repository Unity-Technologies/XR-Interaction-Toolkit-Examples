using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class DestroyObjectWithDelay : MonoBehaviour {

        public float DestroySeconds = 0f;

        // Start is called before the first frame update
        void Start() {
            Destroy(this.gameObject, DestroySeconds);
        }
    }
}
