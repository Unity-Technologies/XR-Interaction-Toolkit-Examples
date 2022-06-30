using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BNG {

    /// <summary>
    /// This is an example of how to highlight an object on hover / activate. This is used in the Demo scene in conjunction with the "PointerEvents" component.
    /// </summary>
    public class DemoCube : MonoBehaviour {

        public Material HighlightMaterial;
        public Material ActiveMaterial;

        // Currently activating the object?
        bool active = false;

        // Currently hovering over the object?
        bool hovering = false;

        Material initialMaterial;
        MeshRenderer render;

        void Start() {
            render = GetComponent<MeshRenderer>();
            initialMaterial = render.sharedMaterial;
        }        

        // Holding down activate
        public void SetActive(PointerEventData eventData) {
            active = true;

            UpdateMaterial();
        }

        // No longer ohlding down activate
        public void SetInactive(PointerEventData eventData) {
            active = false;

            UpdateMaterial();
        }

        // Hovering over our object
        public void SetHovering(PointerEventData eventData) {
            hovering = true;

            UpdateMaterial();
        }

        // No longer hovering over our object
        public void ResetHovering(PointerEventData eventData) {
            hovering = false;
            active = false;

            UpdateMaterial();
        }

        public void UpdateMaterial() {
            if (active) {
                render.sharedMaterial = ActiveMaterial;
            }
            else if (hovering) {
                render.sharedMaterial = HighlightMaterial;
            }
            else {
                render.sharedMaterial = initialMaterial;
            }
        }
    }
}

