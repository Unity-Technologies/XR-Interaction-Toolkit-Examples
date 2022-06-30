using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class GrabbableChild : MonoBehaviour {
        [Tooltip("The Parent Grabbable Object to be grabbed.")]
        public Grabbable ParentGrabbable;
    }
}
