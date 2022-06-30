using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// Add this Component to any Canvas to make sure it can be interacted with in World Space
    /// </summary>
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(Canvas))]
    public class VRCanvas : MonoBehaviour {

        void Start() {
            VRUISystem.Instance.AddCanvas(GetComponent<Canvas>());
        }
    }
}

