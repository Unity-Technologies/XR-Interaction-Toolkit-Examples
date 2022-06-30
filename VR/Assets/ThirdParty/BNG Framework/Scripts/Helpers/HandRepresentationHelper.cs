using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BNG {

    /// <summary>
    /// Only show this Hand Transform if it's distance from "OtherHandTransform" is >= "DistanceToShow"
    /// </summary>
    public class HandRepresentationHelper : MonoBehaviour {

        [Tooltip("The GameObject to be shown or hidden depending on Distance from OtherHandTransform")]
        public Transform HandToToggle;

        [Tooltip("The other Hand Transform used to calculate distance")]
        public Transform OtherHandTransform;

        [Tooltip("Distance required to show this Transform in meters")]
        public float DistanceToShow = 0.1f;

        void Update() {

            if(Vector3.Distance(HandToToggle.position, OtherHandTransform.position) >= DistanceToShow) {
                HandToToggle.gameObject.SetActive(true);
            }
            else {
                HandToToggle.gameObject.SetActive(false);
            }
        }
    }
}

