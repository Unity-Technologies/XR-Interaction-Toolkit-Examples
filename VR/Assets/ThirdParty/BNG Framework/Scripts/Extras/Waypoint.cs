using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class Waypoint : MonoBehaviour {

        public Waypoint Destination;

        void OnDrawGizmosSelected() {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.1f);

            if(Destination != null) {
                Gizmos.DrawLine(transform.position, Destination.transform.position);
                Gizmos.DrawSphere(Destination.transform.position, 0.1f);
            }
        }

    }
}

