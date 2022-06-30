using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class FingerTipCollider : MonoBehaviour {

        [Tooltip("Radius (in meters) of the fingertip to use when checking for collisions during auto-posing. (Default : 0.00875)")]
        [Range(0.0f, 0.02f)]
        public float Radius = 0.00875f;
    }
}

