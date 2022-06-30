using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// An example bow item. Configurable force and damage.
    /// </summary>
    public class Bow : GrabbableEvents {


        [Header("Bow Settings")]

        /// <summary>
        /// How much force to apply to the arrow, multiplied by how far back the bow is pulled
        /// </summary>
        [Tooltip("")]
        public float BowForce = 50f;
        [Tooltip("If True the BowModel Transform will align itself with the grabber holding the arrow")]
        public bool AlignBowToArrow = true;

        [Tooltip("If AlignBowToArrow is true this transform will align itself with the grabber holding the arrow")]
        public Transform BowModel; 

        [Header("Arrow Settings")]
        [Tooltip("Arrow will rotate around this if bow is held in left hand or ArrowRestLeftHanded is null")]
        public Transform ArrowRest;

        /// <summary>
        /// If true, the player can grab a new arrow by holding the trigger down near the knock        
        /// </summary>
        public bool CanGrabArrowFromKnock = true;

        [Tooltip("Name of the prefab used to create an arrow. Must be in a /Resources/ directory.")]
        public string ArrowPrefabName = "Arrow2";

        [Tooltip("Arrow will rotate around this if bow is being held in right hand")]
        public Transform ArrowRestLeftHanded; // Arrow will rotate around this

        public Transform ArrowKnock; // Pull this back

        [Header("Arrow Positioning")]
        public bool IgnoreXPosition = false;
        public bool IgnoreYPosition = false;
        public bool AllowNegativeZ = true;

        [Header("Arrow Grabbing")]
        public bool CanGrabArrow = false;

        [HideInInspector]
        public Grabber ClosestGrabber;
        [HideInInspector]
        public Arrow GrabbedArrow;
        Grabbable arrowGrabbable;
        [HideInInspector]
        public Grabber arrowGrabber; // Which grabber is Grabbing the Arrow
        [HideInInspector]
        public Vector3 LastValidPosition;

        [Header("String Settings")]
        public float MaxStringDistance = 0.3f;
        public float StringDistance = 0;

        public float DrawPercent { get; private set; } = 0;
        private float _lastDrawPercent; // DrawPercent Last Frame
        private float _lastDrawHaptic;
        private float _lastDrawHapticTime; // Last time.time we played a haptic
        private bool playedDrawSound = false;

        Vector3 initialKnockPosition;

        bool holdingArrow = false;
        Grabbable bowGrabbable;

        [Header("Debug Text")]
        public Text PercentageUI;

        // Used for bow haptics
        List<DrawDefinition> drawDefs;

        AudioSource audioSource;

        void Start() {
            initialKnockPosition = ArrowKnock.localPosition;
            bowGrabbable = GetComponent<Grabbable>();
            audioSource = GetComponent<AudioSource>();

            // Define a few haptic positions
            drawDefs = new List<DrawDefinition>() {
                { new DrawDefinition() { DrawPercentage = 30f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 40f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 50f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 60f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 70f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 80f, HapticAmplitude = 0.1f, HapticFrequency = 0.1f } },
                { new DrawDefinition() { DrawPercentage = 90f, HapticAmplitude = 0.1f, HapticFrequency = 0.9f } },
                { new DrawDefinition() { DrawPercentage = 100f, HapticAmplitude = 0.1f, HapticFrequency = 1f } },
            };
        }

        void Update() {

            updateDrawDistance();

            checkBowHaptics();

            // Dropped bow. Make sure arrow has been fired
            if (!bowGrabbable.BeingHeld) {

                // Dropped bow; release arrow
                if(holdingArrow) {
                    ReleaseArrow();
                }

                resetStringPosition();
                return;
            }

            holdingArrow = GrabbedArrow != null;

            // Grab an arrow by holding trigger in grab area
            if (canGrabArrowFromKnock()) {

                GameObject arrow = Instantiate(Resources.Load(ArrowPrefabName, typeof(GameObject))) as GameObject;
                arrow.transform.position = ArrowKnock.transform.position;
                arrow.transform.LookAt(getArrowRest());

                // Use trigger when grabbing from knock
                Grabbable g = arrow.GetComponent<Grabbable>();
                g.GrabButton = GrabButton.Trigger;
                
                // We will apply our own velocity on drop
                g.AddControllerVelocityOnDrop = false;

                GrabArrow(arrow.GetComponent<Arrow>());
            }

            // No arrow, lerp knock back to start
            if (GrabbedArrow == null) {
                resetStringPosition();                
            }

            if(arrowGrabber != null) {
                StringDistance = Vector3.Distance(transform.position, arrowGrabber.transform.position);
            }
            else {
                StringDistance = 0;
            }

            // Move arrow knock, align the arrow
            if (holdingArrow) {
                setKnockPosition();
                alignArrow();
                checkDrawSound();
                checkBowHaptics();

                // Let Go of Trigger, shoot arrow                
                if (getGrabArrowInput() <= 0.2f) {
                    ReleaseArrow();
                }
            }

            alignBow();
        }

        Transform getArrowRest() {

            if(bowGrabbable.GetPrimaryGrabber() != null && bowGrabbable.GetPrimaryGrabber().HandSide == ControllerHand.Right && ArrowRestLeftHanded != null) {
                return ArrowRestLeftHanded;
            }
            
            return ArrowRest;
        }

        bool canGrabArrowFromKnock() {

            // Setting override
            if(!CanGrabArrowFromKnock) {
                return false;
            }

            // Use opposite hand of what's holding the bow
            ControllerHand hand = bowGrabbable.GetControllerHand(bowGrabbable.GetPrimaryGrabber()) == ControllerHand.Left ? ControllerHand.Right : ControllerHand.Left;

            return CanGrabArrow && getTriggerInput(hand) > 0.75f && !holdingArrow;
        }

        float getGrabArrowInput() {
            // If we are holding an arrow, check the arrow details for input
            if (arrowGrabber != null && arrowGrabbable != null) {

                GrabButton grabButton = arrowGrabber.GetGrabButton(arrowGrabbable);

                // Grip Controls
                if (grabButton == GrabButton.Grip) {
                    return getGripInput(arrowGrabber.HandSide);
                }
                // Trigger
                else if (grabButton == GrabButton.Trigger) {
                    return getTriggerInput(arrowGrabber.HandSide);
                }
            }

            return 0;
        }

        float getGripInput(ControllerHand handSide) {
            if (handSide == ControllerHand.Left) {
                return input.LeftGrip;
            }
            else if (handSide == ControllerHand.Right) {
                return input.RightGrip;
            }

            return 0;
        }

        float getTriggerInput(ControllerHand handSide) {
            if (handSide == ControllerHand.Left) {
                return input.LeftTrigger;
            }
            else if (handSide == ControllerHand.Right) {
                return input.RightTrigger;
            }

            return 0;
        }

        void setKnockPosition() {

            // Set knock to hand if within range
            if(StringDistance <= MaxStringDistance) {
                ArrowKnock.position = arrowGrabber.transform.position;
            }
            else {
                ArrowKnock.localPosition = initialKnockPosition;
                ArrowKnock.LookAt(arrowGrabber.transform, ArrowKnock.forward);
                ArrowKnock.position += ArrowKnock.forward * (MaxStringDistance * 0.65f);
            }

            // Constrain position
            if (IgnoreXPosition) {
                ArrowKnock.localPosition = new Vector3(getArrowRest().localPosition.x, ArrowKnock.localPosition.y, ArrowKnock.localPosition.z);
            }
            if (IgnoreYPosition) {
                ArrowKnock.localPosition = new Vector3(ArrowKnock.localPosition.x, 0, ArrowKnock.localPosition.z);
            }

            // Z Position
            if(!AllowNegativeZ && ArrowKnock.localPosition.z > initialKnockPosition.z) {
                ArrowKnock.localPosition = new Vector3(ArrowKnock.localPosition.x, ArrowKnock.localPosition.y, initialKnockPosition.z);
            }
        }

        void checkDrawSound() {
            if(holdingArrow && !playedDrawSound && DrawPercent > 30f) {
                playBowDraw();
                playedDrawSound = true;
            }
        }
        
        void updateDrawDistance() {
            _lastDrawPercent = DrawPercent;

            float knockDistance = Math.Abs(Vector3.Distance(ArrowKnock.localPosition, initialKnockPosition));
            DrawPercent = (knockDistance / MaxStringDistance) * 100;

            if (PercentageUI != null) {                
                PercentageUI.text = (int)DrawPercent + "%";
            }
        }

        void checkBowHaptics() {

            // If we aren't pulling back then skip the check
            // Only apply haptics on pull back
            if (DrawPercent < _lastDrawPercent) {
                return;
            }

            // Don't apply haptics if we just applied them recently
            if(Time.time - _lastDrawHapticTime < 0.11) {
                return;
            }            

            if(drawDefs == null) {
                return;
            }

            DrawDefinition d = drawDefs.FirstOrDefault(x => x.DrawPercentage <= DrawPercent && x.DrawPercentage != _lastDrawHaptic);
            if(d != null && arrowGrabber != null) {
                input.VibrateController(d.HapticFrequency, d.HapticAmplitude, 0.1f, arrowGrabber.HandSide);
                _lastDrawHaptic = d.DrawPercentage;
                _lastDrawHapticTime = Time.time;
            }

        }

        void resetStringPosition() {
            ArrowKnock.localPosition = Vector3.Lerp(ArrowKnock.localPosition, initialKnockPosition, Time.deltaTime * 100);
        }

        protected virtual void alignArrow() {
            GrabbedArrow.transform.parent = this.transform;
            GrabbedArrow.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
            GrabbedArrow.GetComponent<Rigidbody>().isKinematic = true;

            GrabbedArrow.transform.position = ArrowKnock.transform.position;
            GrabbedArrow.transform.LookAt(getArrowRest());
        }

        public Vector3 BowUp = Vector3.forward;

        public float AlignBowSpeed = 20f;
        protected virtual void alignBow() {

            // Bail early
            if (AlignBowToArrow == false || BowModel == null || grab == null || !grab.BeingHeld) {
                return;
            }

            // Reset Alignment
            if(grab != null && grab.BeingHeld) {
                if(holdingArrow) {
                    
                    if (GrabbedArrow != null) {
                        BowModel.transform.rotation = GrabbedArrow.transform.rotation;
                    }
                    else {
                        BowModel.transform.localRotation = Quaternion.Slerp(BowModel.transform.localRotation, Quaternion.identity, Time.deltaTime * AlignBowSpeed);
                    }
                    Vector3 eulers = BowModel.transform.localEulerAngles;
                    eulers.z = 0;

                    BowModel.transform.localEulerAngles = eulers;
                }
                else {
                    BowModel.transform.localRotation = Quaternion.Slerp(BowModel.transform.localRotation, Quaternion.identity, Time.deltaTime * AlignBowSpeed);
                }
            }
        }

        public virtual void ResetBowAlignment() {
            if (BowModel != null) {
                BowModel.localEulerAngles = Vector3.zero;
            }
        }

        public void GrabArrow(Arrow arrow) {

            arrowGrabber = ClosestGrabber;

            // Signal the grabbable that we're being held
            GrabbedArrow = arrow.GetComponent<Arrow>();
            GrabbedArrow.ShaftCollider.enabled = false;

            arrowGrabbable = arrow.GetComponent<Grabbable>();

            if (arrowGrabbable) {
                arrowGrabbable.GrabItem(arrowGrabber);
                arrowGrabber.HeldGrabbable = arrowGrabbable;
                arrowGrabbable.AddControllerVelocityOnDrop = false;
            }


            Collider playerCollder = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<Collider>();
            if(playerCollder) {
                Physics.IgnoreCollision(GrabbedArrow.ShaftCollider, playerCollder);
            }

            holdingArrow = true;
        }        

        public void ReleaseArrow() {

            // Start sound immediately
            playBowRelease();

            // No longer "holding" the arrow
            if (arrowGrabbable) {
                // Reset Arrow Grab to Grip
                arrowGrabbable.GrabButton = GrabButton.Grip;

                arrowGrabbable.DropItem(false, true);

                // We can apply velocity now
                arrowGrabbable.AddControllerVelocityOnDrop = true;
            }

            // Calculate shot force
            float shotForce = BowForce * StringDistance;
            GrabbedArrow.ShootArrow(GrabbedArrow.transform.forward * shotForce);

            // Make sure hands are showing if we hid them
            arrowGrabber.ResetHandGraphics();

            resetArrowValues();
        }

        public override void OnRelease() {
            ResetBowAlignment();
            resetStringPosition();
        }

        // Make sure all starting values are reset
        void resetArrowValues() {
            GrabbedArrow = null;
            arrowGrabbable = null;
            arrowGrabber = null;
            holdingArrow = false;
            playedDrawSound = false;
        }

        void playSoundInterval(float fromSeconds, float toSeconds, float volume) {
            if(audioSource) {

                if(audioSource.isPlaying) {
                    audioSource.Stop();
                }

                audioSource.pitch = Time.timeScale;
                audioSource.time = fromSeconds;
                audioSource.volume = volume;
                audioSource.Play();
                audioSource.SetScheduledEndTime(AudioSettings.dspTime + (toSeconds - fromSeconds));
            }
        }

        void playBowDraw() {
            playSoundInterval(0, 1.66f, 0.4f);
        }

        void playBowRelease() {
            playSoundInterval(1.67f, 2.2f, 0.3f);
        }
    }

    /// <summary>
    /// A list of how and when to play a haptic according to DrawPercentage
    /// </summary>
    public class DrawDefinition {
        public float DrawPercentage { get; set; }
        public float HapticAmplitude { get; set; }
        public float HapticFrequency { get; set; }
    }
}

