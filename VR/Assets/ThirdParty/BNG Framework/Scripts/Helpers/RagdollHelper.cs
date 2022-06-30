using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BNG {
    public class RagdollHelper : MonoBehaviour {

        Transform player;
        List<Collider> colliders;
        Collider playerCol;

        // Start is called before the first frame update
        void Start() {
            player = GameObject.FindGameObjectWithTag("Player").transform;
            playerCol = player.GetComponentInChildren<Collider>();

            colliders = GetComponentsInChildren<Collider>().ToList();

            foreach(var col in colliders) {
                Physics.IgnoreCollision(col, playerCol, true);
            }
        }
    }
}

