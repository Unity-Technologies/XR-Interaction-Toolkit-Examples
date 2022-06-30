using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {
    public class VRKeyboardKeyCollider : MonoBehaviour {

        public float PressedInZValue = 0.01f;
        public float PressInSpeed = 15f;

        UnityEngine.UI.Button uiButton;

        int itemsInTrigger = 0;
        bool wasSelected = false;

        void Awake() {
            uiButton = GetComponentInParent<UnityEngine.UI.Button>();
        }

        void Update() {

            if (itemsInTrigger > 0) {
                transform.parent.localPosition = Vector3.Lerp(transform.parent.localPosition, new Vector3(transform.parent.localPosition.x, transform.parent.localPosition.y, PressedInZValue), Time.deltaTime * PressInSpeed);

                if(!wasSelected) {
                    uiButton.onClick.Invoke();
                    // uiButton.Select();

                    wasSelected = true;
                }
            }
            else {
                transform.parent.localPosition = Vector3.Lerp(transform.parent.localPosition, new Vector3(transform.parent.localPosition.x, transform.parent.localPosition.y, 0), Time.deltaTime * PressInSpeed);
                wasSelected = false;
            }
        }


        void OnTriggerEnter(Collider other) {
            //if (other.GetComponent<UITrigger>() != null || other.GetComponent<Grabber>() != null) {
            if (other.GetComponent<UITrigger>() != null) {
                itemsInTrigger++;
            }
        }

        void OnTriggerExit(Collider other) {
            if (other.GetComponent<UITrigger>() != null) {
                itemsInTrigger--;
            }
        }
    }
}

