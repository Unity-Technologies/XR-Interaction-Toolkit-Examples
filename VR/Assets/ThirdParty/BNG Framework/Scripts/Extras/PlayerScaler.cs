using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// An example script for how to scale a player in VR
    /// </summary>
    public class PlayerScaler : MonoBehaviour {

        public List<Transform> ScaleObjects;
        public float Scale = 1f;

        public float scaleSpeed = 0.1f;

        // Scale using Right Thumbstick axis
        void Update() {
            if (InputBridge.Instance.RightThumbstickAxis.y < -0.5f || Input.GetKey(KeyCode.H)) {
                Scale -= Time.deltaTime * 1;
            }
            else if (InputBridge.Instance.RightThumbstickAxis.y > 0.5f || Input.GetKey(KeyCode.J)) {
                Scale += Time.deltaTime * 1;
            }

            Scale = Mathf.Clamp(Scale, 0.1f, 2f);

            foreach (var t in ScaleObjects) {
                t.localScale = new Vector3(Scale, Scale, Scale);
            }
        }
    }
}

