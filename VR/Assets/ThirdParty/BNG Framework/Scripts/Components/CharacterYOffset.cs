using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    public class CharacterYOffset : MonoBehaviour {        

        // This is used to offset a FinalIK character by it's parent. This fixes some positional issues.
        void LateUpdate() {
            float yOffset = transform.parent.localPosition.y;
            transform.localPosition = new Vector3(transform.localPosition.x, -1 - yOffset, transform.localPosition.z);
        }
    }
}
