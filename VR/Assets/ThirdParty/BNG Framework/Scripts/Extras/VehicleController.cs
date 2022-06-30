using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {
    public class VehicleController : MonoBehaviour {

        [Header("Engine Properties")]
        public float MotorTorque = 500f;
        public float MaxSpeed = 30f;

        public float MaxSteeringAngle = 45f;

        [Header("Steering Grabbable")]
        [Tooltip("If true and SteeringGrabbable is being held, the right / left trigger will act as input for acceleration / defceleration.")]
        public bool CheckTriggerInput = true;
        public Grabbable SteeringGrabbable;

        [Header("Engine Status")]
        [Tooltip("Is the Engine on and ready for input. If false, engine will need to be started first.")]
        public bool EngineOn = false;
        

        [Tooltip("How long it takes to start the engine")]
        public float CrankTime = 0.1f;

        [Header("Speedometer")]
        [Tooltip("Output the current speed to this label if specified")]
        public Text SpeedLabel;

        [Header("Audio Setup")]
        public AudioSource EngineAudio;

        [Tooltip("Sound to play / loop when EngineOn = true. Pitch will be altered according to speed.")]
        public AudioClip IdleSound;

        [Tooltip("If specified this clip will play before the engine is turned on. Clip to play when starting the Engine.")]
        public AudioClip CrankSound;

        public AudioClip CollisionSound;

        [HideInInspector]
        public float SteeringAngle = 0;
        [HideInInspector]
        public float MotorInput = 0; // Between 0-1. Multiplied times MotorTorque
        [HideInInspector]
        public float CurrentSpeed;


        [Header("Wheel Configuration")]
        public List<WheelObject> Wheels;

        Vector3 initialPosition;
        Rigidbody rb;

        bool wasHoldingSteering, isHoldingSteering;

        void Start() {
            rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;
        }

        // Update is called once per frame
        void Update() {

            isHoldingSteering = SteeringGrabbable != null && SteeringGrabbable.BeingHeld;

            if (CheckTriggerInput) {
                GetTorqueInputFromTriggers();
            }

            // Check if we need to crank the engine
            if(Mathf.Abs(MotorInput) > 0.01f && !EngineOn) {
                CrankEngine();
            }

            // Need to let engine finish cranking
            if (crankingEngine) {
                return;
            }

            UpdateEngineAudio();

            if (SpeedLabel != null) {
                SpeedLabel.text = CurrentSpeed.ToString("n0");
            }

            CheckOutOfBounds();

            wasHoldingSteering = isHoldingSteering;
        }

        // Starts the motor if it isn't already on
        public virtual void CrankEngine() {

            if (crankingEngine || EngineOn) {
                return;
            }

            StartCoroutine(crankEngine());
        }

        protected bool crankingEngine = false;

        IEnumerator crankEngine() {
            crankingEngine = true;

            if(CrankSound != null) {
                EngineAudio.clip = CrankSound;
                EngineAudio.loop = false;
                EngineAudio.Play();
            }

            yield return new WaitForSeconds(CrankTime);

            // Switch to idle sound
            if(IdleSound != null) {
                EngineAudio.clip = IdleSound;
                EngineAudio.loop = true;
                EngineAudio.Play();
            }

            yield return new WaitForEndOfFrame();

            crankingEngine = false;
            EngineOn = true;
        }

        // Did we fall under the world?
        public virtual void CheckOutOfBounds() {
            if(transform.position.y < -500f) {
                transform.position = initialPosition;
            }
        }

        public virtual void GetTorqueInputFromTriggers() {
            // Right Trigger Accelerate, Left Trigger Brake
            if(isHoldingSteering) {
                SetMotorTorqueInput(InputBridge.Instance.RightTrigger - InputBridge.Instance.LeftTrigger);
            }
            // Nothing Holding the steering wheel. Set torque to 0
            else if(wasHoldingSteering && !isHoldingSteering) {
                SetMotorTorqueInput(0);
            }
        }
        
        void FixedUpdate() {

            // Update speedometer
            CurrentSpeed = correctValue(rb.velocity.magnitude * 3.6f);

            UpdateWheelTorque();
        }

        public virtual void UpdateWheelTorque() {
            float torqueInput = EngineOn ? MotorInput : 0;

            // Add torque / rotate wheels
            for (int x = 0; x < Wheels.Count; x++) {
                WheelObject wheel = Wheels[x];

                // Steering
                if (wheel.ApplySteering) {
                    wheel.Wheel.steerAngle = MaxSteeringAngle * SteeringAngle;
                }

                // Torque
                if (wheel.ApplyTorque) {
                    wheel.Wheel.motorTorque = MotorTorque * torqueInput;
                }

                UpdateWheelVisuals(wheel);
            }
        }

        public virtual void SetSteeringAngle(float steeringAngle) {
            SteeringAngle = steeringAngle;
        }

        public virtual void SetSteeringAngleInverted(float steeringAngle) {
            SteeringAngle = steeringAngle * -1;
        }

        public virtual void SetSteeringAngle(Vector2 steeringAngle) {
            SteeringAngle = steeringAngle.x;
        }

        public virtual void SetSteeringAngleInverted(Vector2 steeringAngle) {
            SteeringAngle = -steeringAngle.x;
        }

        public virtual void SetMotorTorqueInput(float input) {
            MotorInput = input;
        }

        public virtual void SetMotorTorqueInputInverted(float input) {
            MotorInput = -input;
        }

        public virtual void SetMotorTorqueInput(Vector2 input) {
            MotorInput = input.y;
        }

        public virtual void SetMotorTorqueInputInverted(Vector2 input) {
            MotorInput = -input.y;
        }

        public virtual void UpdateWheelVisuals(WheelObject wheel) {
            // Update Wheel position / rotation based on WheelColliders World Pose
            if(wheel != null && wheel.WheelVisual != null) {
                Vector3 position;
                Quaternion rotation;
                wheel.Wheel.GetWorldPose(out position, out rotation);

                wheel.WheelVisual.transform.position = position;
                wheel.WheelVisual.transform.rotation = rotation;
            }
        }

        public virtual void UpdateEngineAudio() {
            if (EngineAudio && EngineOn) {
                EngineAudio.pitch = Mathf.Clamp(0.5f + (CurrentSpeed / MaxSpeed), -0.1f, 3f);
            }
        }

        void OnCollisionEnter(Collision collision) {
            float colVelocity = collision.relativeVelocity.magnitude;
            if(colVelocity > 0.1f) {
                VRUtils.Instance.PlaySpatialClipAt(CollisionSound, collision.GetContact(0).point, 1f);
            }
        }

        float correctValue(float inputValue) {
            return (float)System.Math.Round(inputValue * 1000f) / 1000f;
        }
    }

    [System.Serializable]
    public class WheelObject {
        public WheelCollider Wheel;
        public Transform WheelVisual;
        public bool ApplyTorque;
        public bool ApplySteering;
    }
}

