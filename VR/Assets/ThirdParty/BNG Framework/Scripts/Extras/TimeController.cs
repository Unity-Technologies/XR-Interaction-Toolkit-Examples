using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BNG {

    /// <summary>
    /// Press Y to slow time by modifying Time.timeScale and Time.fixedDeltaTime
    /// </summary>
    public class TimeController : MonoBehaviour {

        /// <summary>
        /// Timescale to slow down to if slow down key is pressed
        /// </summary>
        [Tooltip("Timescale to slow down to if slow down key is pressed")]
        public float SlowTimeScale = 0.5f;

        /// <summary>
        /// If true, Y Button will always slow time. Useful for debugging. Otherwise call SlowTime / ResumeTime yourself, or use a Unity InputActionReference
        /// </summary>
        [Tooltip("If true, Y Button will always slow time. Useful for debugging. Otherwise call SlowTime / ResumeTime yourself")]
        public bool YKeySlowsTime = true;

        [Tooltip("Input Action used to initiate slow time")]
        public InputActionReference SlowTimeAction;       

        [Tooltip("(Optional) Play this clip when starting to slow time")]
        public AudioClip SlowTimeClip;

        [Tooltip("(Optional) Play this clip when ending slow mo")]
        public AudioClip SpeedupTimeClip;

        /// <summary>
        /// If true, will set Time.fixedDeltaTime to the device refresh rate
        /// </summary>
        [Tooltip("If true, will set Time.fixedDeltaTime to the device refresh rate")]
        public bool SetFixedDelta = false;

        [Tooltip("If true, will check for input in Update to slow down time. If false you'll need to call SlowTime() / ResumeTime() manually from script")]
        public bool CheckInput = true;

        public bool TimeSlowing
        {
            get { return _slowingTime;  }
        }
        bool _slowingTime = false;
        bool routineRunning = false;

        float originalFixedDelta;
        AudioSource audioSource;

        public bool ForceTimeScale = false;

        // Start is called before the first frame update
        void Start() {
            
            if(SetFixedDelta) {
                Time.fixedDeltaTime = (Time.timeScale / UnityEngine.XR.XRDevice.refreshRate);
            }

            originalFixedDelta = Time.fixedDeltaTime;

            audioSource = GetComponent<AudioSource>();
        }

        void Update() {

            if(CheckInput) {
                if (SlowTimeInputDown() || ForceTimeScale) {
                    SlowTime();
                }
                else {
                    ResumeTime();
                }
            }
        }


        /// <summary>
        /// Returns true if SlowTimeAction is being held down
        /// </summary>
        /// <returns></returns>
        public virtual bool SlowTimeInputDown() {
            // Check default Y Key
            if ((YKeySlowsTime && InputBridge.Instance.YButton)) {
                return true;
            }
            
            // Check for Unity Input Action
            if (SlowTimeAction != null) {
                return SlowTimeAction.action.ReadValue<float>() > 0f;
            }

            return false;
        }

        public void SlowTime() {
           
            if(!_slowingTime) {

                // Make sure we aren't running a routine
                if(resumeRoutine != null) {
                    StopCoroutine(resumeRoutine);
                }

                // Play Slow time clip
                audioSource.clip = SlowTimeClip;
                audioSource.Play();

                // Haptics
                if(SpeedupTimeClip) {
                    InputBridge.Instance.VibrateController(0.1f, 0.2f, SpeedupTimeClip.length, ControllerHand.Left);
                }

                Time.timeScale = SlowTimeScale;
                Time.fixedDeltaTime = originalFixedDelta * Time.timeScale;

                _slowingTime = true;
            }
        }

        private IEnumerator resumeRoutine;
        public void ResumeTime() {
            // toggled over; play audio cue
            // Don't resume until we're done playing the initial sound clip
            if(_slowingTime && !audioSource.isPlaying && !routineRunning) {

                resumeRoutine = resumeTimeRoutine();
                StartCoroutine(resumeRoutine);
            }
        }

        IEnumerator resumeTimeRoutine() {
            routineRunning = true;

            audioSource.clip = SpeedupTimeClip;
            audioSource.Play();

            InputBridge.Instance.VibrateController(0.1f, 0.2f, SpeedupTimeClip.length, ControllerHand.Left);

            // Wait for a split second before resuming time again
            yield return new WaitForSeconds(0.35f);

            Time.timeScale = 1;
            Time.fixedDeltaTime = originalFixedDelta;

            _slowingTime = false;
            routineRunning = false;
        }
    }
}

