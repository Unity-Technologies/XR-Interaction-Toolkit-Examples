using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class IgnoreColliders : MonoBehaviour {

        public List<Collider> CollidersToIgnore;

        // Start is called before the first frame update
        void Start() {
            var thisCol = GetComponent<Collider>();
            if(CollidersToIgnore != null) {
                foreach(var col in CollidersToIgnore) {
                    if(col && col.enabled) {
                        Physics.IgnoreCollision(thisCol, col, true);
                    }
                }
            }
        }
    }
}