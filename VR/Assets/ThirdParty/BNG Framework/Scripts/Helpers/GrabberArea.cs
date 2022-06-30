using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {

    /// <summary>
    /// Keeps a List of all Grabbers within this Trigger
    /// </summary>
    public class GrabberArea : MonoBehaviour {

        public Grabber InArea;

        public List<Grabber> grabbersInArea;

        private void Update() {
            InArea = GetOpenGrabber();
        }

        public Grabber GetOpenGrabber() {
            if(grabbersInArea != null && grabbersInArea.Count > 0) {
                foreach (var g in grabbersInArea) {
                    if(!g.HoldingItem) {
                        return g;
                    }
                }
            }

            return null;
        }

        void OnTriggerEnter(Collider other) {

            Grabber grab = other.GetComponent<Grabber>();
            if (grab != null) {

                if(grabbersInArea == null) {
                    grabbersInArea = new List<Grabber>();
                }

                if(!grabbersInArea.Contains(grab)) {
                    grabbersInArea.Add(grab);
                }
            }
        }

        void OnTriggerExit(Collider other) {
            Grabber grab = other.GetComponent<Grabber>();
            if (grab != null) {

                if (grabbersInArea == null) {
                    return;
                }

                if (grabbersInArea.Contains(grab)) {
                    grabbersInArea.Remove(grab);
                }
            }
        }
    }
}
