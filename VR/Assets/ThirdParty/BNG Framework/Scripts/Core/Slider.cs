using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BNG {
    /// <summary>
    /// Helper class to interact with physical sliders
    /// </summary>
    public class Slider : MonoBehaviour {

        /// <summary>
        /// How far away slider has moved from inital RangeLow position
        /// </summary>
        public float SlidePercentage
        {
            get
            {
                return _slidePercentage;
            }
        }
        private float _slidePercentage;

        /// <summary>
        /// Event to call if slider's value changes
        /// </summary>
        public FloatEvent onSliderChange;
        
        float lastSliderPercentage;
        float slideRangeLow = -0.15f;
        float slideRangeHigh = 0.15f;
        float slideRange;

        void Start() {
            ConfigurableJoint cj = GetComponent<ConfigurableJoint>();
            if (cj) {
                slideRangeLow = cj.linearLimit.limit * -1;
                slideRangeHigh = cj.linearLimit.limit;
            }

            slideRange = slideRangeHigh - slideRangeLow;
        }

        void Update() {
            
            _slidePercentage = ( (transform.localPosition.x - 0.001f) + slideRangeHigh) / slideRange;
            _slidePercentage = Mathf.Ceil(_slidePercentage * 100);

            if (_slidePercentage != lastSliderPercentage) {
                OnSliderChange(_slidePercentage);
            }

            lastSliderPercentage = _slidePercentage;
        }

        // Callback for lever percentage change
        public virtual void OnSliderChange(float percentage) {

            if(onSliderChange != null) {
                onSliderChange.Invoke(percentage);
            }
        }
    }
}