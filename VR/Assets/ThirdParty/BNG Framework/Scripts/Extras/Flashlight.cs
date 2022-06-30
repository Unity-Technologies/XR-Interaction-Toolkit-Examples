using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// A simple Grabbable example that toggles a light source on and off
    /// </summary>
    public class Flashlight : GrabbableEvents {

        public Light SpotLight;
        public Transform LightSwitch;

        Vector3 originalSwitchPosition;

        // Start is called before the first frame update
        void Start() {
            originalSwitchPosition = LightSwitch.transform.localPosition;
        }

        public override void OnTrigger(float triggerValue) {

            SpotLight.enabled = triggerValue > 0.2f;

            LightSwitch.localPosition = new Vector3(originalSwitchPosition.x * triggerValue, originalSwitchPosition.y, originalSwitchPosition.z);

            base.OnTrigger(triggerValue);
        }

        public override void OnTriggerUp() {

            SpotLight.enabled = false;

            LightSwitch.localPosition = originalSwitchPosition;

            base.OnTriggerUp();
        }
    }
}

