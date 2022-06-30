using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BNG {

    public enum MovementVector {
        HMD,
        Controller
    }    

    public class SmoothLocomotion : MonoBehaviour {

        public PlayerControllerType ControllerType = PlayerControllerType.CharacterController;

        [Header("CharacterController Settings : ")]
        public float MovementSpeed = 1.25f;

        [Header("Rigidbody Settings : ")]
        [Tooltip("How much force to apply to the rigidbody when the player is grounded")]
        public float MovementForce = 500f;

        [Tooltip("Maximium velocity of the player's rigidbody in X, Z. Used to constrain max speed movement.")]
        public float MaxHorizontalVelocity = 5f;

        [Tooltip("Maximium velocity of the player's rigidbody on the Y axis. Used to constrain max vertical movement, such as jumping and falling.")]
        public float MaxVerticalVelocity = 10f;

        [Tooltip("How far up from the characters feet is considered a step. Anything below this will be considered grounded.")]
        public float StepHeight = 0.1f;

        [Tooltip("Maximium angle a slope can be to be considered grounded. Negated if contact distance from the player's feet is less than StepHeight.")]
        public float MaxSlopeAngle = 45f;

        [Tooltip("Physics Material to apply to the sphere collider while moving. Use this to dynamically adjust friction and bounciness.")]
        public PhysicMaterial MovementMaterial;

        [Tooltip("Physics Material to apply to the sphere collider when no controls are being issues. Use this to slow t he player down, or allow them to slide across surfaces.")]
        public PhysicMaterial FrictionMaterial;

        [Tooltip("How much drag to apply to the player while moving")]
        public float MovementDrag = 1f;

        [Tooltip("How much drag to apply to the player while standing still. Used to slow down the player or prevent from sliding down a hill.")]
        public float StaticDrag = 5f;

        [Tooltip("How much drag to apply to the player when in the air / not grounded.")]
        public float AirDrag = 1f;

        [Header("Forward Direction : ")]
        [Tooltip("(Optional) If specified, this transform's forward direction will determine the movement direction ")]
        public Transform ForwardDirection;

        [Header("Input : ")]
        [Tooltip("Set to false if you do not want to respond to input commands. (For example, if paused or in a menu)")]
        public bool AllowInput = true;

        [Header("Input : ")]
        [Tooltip("Set to false if you do not want SmoothLocomotion to update movement at all.")]
        public bool UpdateMovement = true;

        [Tooltip("Used to determine which direction to move. Example : Left Thumbstick Axis or Touchpad. ")]
        public List<InputAxis> inputAxis = new List<InputAxis>() { InputAxis.LeftThumbStickAxis };

        [Tooltip("Input Action used to affect movement")]
        public InputActionReference MoveAction;

        [Tooltip("If true, movement events will only be sent if the Application has focus (Or Play window, if running in the Unity Editor)")]
        public bool RequireAppFocus = true;

        [Header("Sprint : ")]
        public float SprintSpeed = 1.5f;

        [Tooltip("The key(s) to use to initiate sprint. You can also override the SprintKeyDown() function to determine your sprint criteria.")]
        public List<ControllerBinding> SprintInput = new List<ControllerBinding>() { ControllerBinding.None };

        [Tooltip("Unity Input Action used to enable sprinting")]
        public InputActionReference SprintAction;

        [Header("Strafe : ")]
        public float StrafeSpeed = 1f;
        public float StrafeSprintSpeed = 1.25f;

        [Header("Jump : ")]
        [Tooltip("Amount of 'force' to apply to the player during Jump")]
        public float JumpForce = 3f;

        [Tooltip("The key(s) to use to initiate a jump. You can also override the CheckJump() function to determine your jump criteria.")]
        public List<ControllerBinding> JumpInput = new List<ControllerBinding>() { ControllerBinding.None };

        [Tooltip("Unity Input Action used to initiate a jump")]
        public InputActionReference JumpAction;

        [Header("Air Control : ")]
        [Tooltip("Can the player move when not grounded? Set to true if you want to be able to move the joysticks and have the player respond to input even when not grounded.")]
        public bool AirControl = true;

        [Tooltip("How fast the player can move in the air if AirControl = true. Example : 0.5 = Player will move at half the speed of MovementSpeed")]
        public float AirControlSpeed = 1f;

        BNGPlayerController playerController;
        CharacterController characterController;
        Rigidbody playerRigid;
        SphereCollider playerSphere;

        // Left / Right
        float movementX;

        // Up / Down
        float movementY;

        // Forwards / Backwards
        float movementZ;

        private float _verticalSpeed = 0; // Keep track of vertical speed

        /// <summary>
        /// Any movement to apply after controller movement has been calculated
        /// </summary>
        Vector3 additionalMovement;

        #region Events
        public delegate void OnBeforeMoveAction();
        public static event OnBeforeMoveAction OnBeforeMove;

        public delegate void OnAfterMoveAction();
        public static event OnAfterMoveAction OnAfterMove;
        #endregion

        public virtual void Update() {
            CheckControllerReferences();
            UpdateInputs();

            if(UpdateMovement) {
                MoveCharacter();
            }
        }

        public virtual void FixedUpdate() {
            if (UpdateMovement && ControllerType == PlayerControllerType.Rigidbody) {
                MoveRigidCharacter();

                // Reset GroundContacts. This will be updated in OnCollisionStay
                GroundContacts = 0;
            }
        }

        bool playerInitialized = false;

        public virtual void CheckControllerReferences() {
            // Component may be called while disabled, so check for references here
            if (playerController == null) {
                playerController = GetComponentInParent<BNGPlayerController>();
            }

            if(playerInitialized == false) {

                // Check CharacterController initialization
                if (ControllerType == PlayerControllerType.CharacterController && characterController == null) {
                    SetupCharacterController();
                    
                    playerInitialized = true;
                }

                // Rigidbody Initialization
                if (ControllerType == PlayerControllerType.Rigidbody && playerRigid == null) {
                    playerRigid = GetComponent<Rigidbody>();
                    playerSphere = GetComponent<SphereCollider>();

                    // If it is still null, then we'll need to setup the player rigid body
                    if (playerRigid == null) {
                        SetupRigidbodyPlayer();
                    }
                }
            }
        }


        public virtual void UpdateInputs() {

            // Start by resetting our previous frame's inputs
            movementX = 0;
            movementY = 0;
            movementZ = 0;

            // Keep values zeroed out if not allowing input
            if(AllowInput == false) {
                return;
            }

            // Start with VR Controller Input
            Vector2 primaryAxis = GetMovementAxis();
            if (IsGrounded()) {
                movementX = primaryAxis.x;
                movementZ = primaryAxis.y;
            }
            else if(AirControl) {
                movementX = primaryAxis.x * AirControlSpeed;
                movementZ = primaryAxis.y * AirControlSpeed;
            }

            // Add Jump Force
            if (CheckJump()) {
                // Add movement directly to CC type
                if(ControllerType == PlayerControllerType.CharacterController) {
                    movementY += JumpForce;
                }
                else if (ControllerType == PlayerControllerType.Rigidbody) {
                    DoRigidBodyJump();
                }
            }

            // Attach any additional speed
            if(CheckSprint()) {
                movementX *= StrafeSprintSpeed;
                movementZ *= SprintSpeed;
            }
            else {
                movementX *= StrafeSpeed;
                movementZ *= MovementSpeed;
            }            
        }

        float lastJumpTime;
        float lastMoveTime;

        public virtual void DoRigidBodyJump() {

            if(Time.time - lastJumpTime > 0.2f) {               

                playerRigid.AddForce(new Vector3(playerRigid.velocity.x, JumpForce, playerRigid.velocity.z), ForceMode.VelocityChange);

                lastJumpTime = Time.time;
            }
        }

        public virtual Vector2 GetMovementAxis() {

            // Use the largest, non-zero value we find in our input list
            Vector3 lastAxisValue = Vector3.zero;

            // Check raw input bindings
            if(inputAxis != null) {
                for (int i = 0; i < inputAxis.Count; i++) {
                    Vector3 axisVal = InputBridge.Instance.GetInputAxisValue(inputAxis[i]);

                    // Always take this value if our last entry was 0. 
                    if (lastAxisValue == Vector3.zero) {
                        lastAxisValue = axisVal;
                    }
                    else if (axisVal != Vector3.zero && axisVal.magnitude > lastAxisValue.magnitude) {
                        lastAxisValue = axisVal;
                    }
                }
            }

            // Check Unity Input Action if we have application focus
            bool hasRequiredFocus = RequireAppFocus == false || RequireAppFocus && Application.isFocused;
            if (MoveAction != null && hasRequiredFocus) {
                Vector3 axisVal = MoveAction.action.ReadValue<Vector2>();

                // Always take this value if our last entry was 0. 
                if (lastAxisValue == Vector3.zero) {
                    lastAxisValue = axisVal;
                }
                else if (axisVal != Vector3.zero && axisVal.magnitude > lastAxisValue.magnitude) {
                    lastAxisValue = axisVal;
                }
            }

            return lastAxisValue;
        }        

        public virtual void MoveCharacter() {

            // Bail early if no elligible for movement
            if (UpdateMovement == false) {
                return;
            }

            Vector3 moveDirection = new Vector3(movementX, movementY, movementZ);

            if(ForwardDirection != null) {
                moveDirection = ForwardDirection.TransformDirection(moveDirection);
            }
            else {
                moveDirection = transform.TransformDirection(moveDirection);
            }

            // Check for jump value
            if (playerController != null && playerController.IsGrounded()) {
                // Reset jump speed if grounded
                _verticalSpeed = 0;
                if (CheckJump()) {
                    _verticalSpeed = JumpForce;
                }
            }

            moveDirection.y = _verticalSpeed;

            if(playerController) {
                playerController.LastPlayerMoveTime = Time.time;
            }

            if(moveDirection != Vector3.zero) {
                MoveCharacter(moveDirection * Time.deltaTime);
            }
        }

        public virtual void MoveRigidCharacter(Vector3 moveTo) {
            // Incremental move not yet implemented
        }

        public virtual void MoveRigidCharacter() {

            float maxVelocityChange = 10f;

            if (playerRigid) {
                Vector3 moveDirection = new Vector3(movementX, movementY, movementZ);
                
                if (ForwardDirection != null) {
                    moveDirection = ForwardDirection.TransformDirection(moveDirection);
                }
                else {
                    moveDirection = transform.TransformDirection(moveDirection);
                }

                Vector3 targetVelocity = moveDirection * MovementForce;
                Vector3 movement = Vector3.zero;

                // Apply a force that attempts to reach our target velocity
                Vector3 currentVelocity = playerRigid.velocity;
                Vector3 velocityTarget = (targetVelocity - currentVelocity);
                bool recentlyJumped = Time.time - lastJumpTime < 0.1f;

                // Do Grounded Movement
                if (IsGrounded()) {

                    // Do movement if grounded and not recently jumped
                    if(!recentlyJumped) {
                        if (maxVelocityChange > 0) {
                            velocityTarget.x = Mathf.Clamp(velocityTarget.x, -maxVelocityChange, maxVelocityChange);
                            // velocityChange.y = Mathf.Clamp(velocityChange.y, -maxVelocityChange, maxVelocityChange);
                            velocityTarget.z = Mathf.Clamp(velocityTarget.z, -maxVelocityChange, maxVelocityChange);
                        }

                        // Adjust physics material if applying movement or waiting to come to a stop
                        if (moveDirection.magnitude > 0.001f) {
                            if(playerSphere) {
                                playerSphere.material = MovementMaterial;
                            }

                            playerRigid.drag = MovementDrag;
                        }
                        else {
                            if (playerSphere) {
                                playerSphere.material = FrictionMaterial;
                            }

                            playerRigid.drag = StaticDrag;
                        }

                        // Apply movement
                        if (moveDirection.magnitude > 0.001f) {
                            playerRigid.AddForce(velocityTarget, ForceMode.VelocityChange);
                        }
                    }
                }
                // Air Control Movement
                else {

                    playerRigid.drag = AirDrag;

                    if (AirControl) {

                        if (!recentlyJumped) {
                            Vector3 move = (targetVelocity) * AirControlSpeed;
                            move.y = 0;

                            playerRigid.AddForce(move, ForceMode.Acceleration);
                        }
                    }
                }

                //  Cap our min / max velocity. 
                var adjustedVelocity = new Vector3(playerRigid.velocity.x, 0, playerRigid.velocity.z);
                if(adjustedVelocity.magnitude > MaxHorizontalVelocity) {
                    adjustedVelocity = Vector3.ClampMagnitude(adjustedVelocity, MaxHorizontalVelocity);

                    adjustedVelocity = new Vector3(adjustedVelocity.x, playerRigid.velocity.y, adjustedVelocity.z);

                    // Apply changes if there was a difference
                    playerRigid.velocity = adjustedVelocity;
                }

                // Clamp the Y axis separately
                if (Mathf.Abs(playerRigid.velocity.y) > MaxVerticalVelocity) {
                    playerRigid.velocity = new Vector3(playerRigid.velocity.x, Mathf.Clamp(playerRigid.velocity.y, -MaxVerticalVelocity, MaxVerticalVelocity), playerRigid.velocity.z);
                }
            }
        }

        public float Magnitude;

        public virtual void MoveCharacter(Vector3 motion) {

            // Can bail immediately if no movement is required
            if(motion == null || motion == Vector3.zero) {
                return;
            }

            Magnitude = (float)Math.Round(motion.magnitude * 1000f) / 1000f;

            bool callEvents = Magnitude > 0.0f;

            CheckControllerReferences();

            // Call any Before Move Events
            if(callEvents) {
                OnBeforeMove?.Invoke();
            }

            // CharacterController Movement Type
            if(ControllerType == PlayerControllerType.CharacterController) {
                if (characterController && characterController.enabled) {
                    characterController.Move(motion);
                }
            }
            // RigidBody Movement Type moved in Fixed Update
            else if (ControllerType == PlayerControllerType.Rigidbody) {
                // playerRigid.MovePosition(transform.position + motion * Time.deltaTime);
            }

            // Call any After Move Events
            if (callEvents) {
                OnAfterMove?.Invoke();
            }
        }

        public virtual bool CheckJump() {

            // Don't jump if not grounded
            if (!IsGrounded()) {
                return false;
            }

            // Check for bound controller button
            for (int x = 0; x < JumpInput.Count; x++) {
                if (InputBridge.Instance.GetControllerBindingValue(JumpInput[x])) {
                    return true;
                }
            }

            // Check Unity Input Action value
            if (JumpAction != null && JumpAction.action.ReadValue<float>() > 0) {
                return true;
            }

            return false;
        }

        public virtual bool CheckSprint() {

            // Check for bound controller button
            for (int x = 0; x < SprintInput.Count; x++) {
                if (InputBridge.Instance.GetControllerBindingValue(SprintInput[x])) {
                    return true;
                }
            }

            // Check Unity Input Action
            if (SprintAction != null) {
                return SprintAction.action.ReadValue<float>() == 1f;
            }

            return false;
        }

        public virtual bool IsGrounded() { 

            // PlayerController has a IsGrounded method we can check
            if(playerController && playerController.IsGrounded()) {
                return true;
            }
            // OnCollision Contacts
            else if(GroundContacts > 0) {
                return true;
            }

            // Could use raycast, oncollisionstay, etc.
            return false;
        }

        public virtual void SetupCharacterController() {
            playerRigid = GetComponent<Rigidbody>();

            BNGPlayerController bng = GetComponent<BNGPlayerController>();
            if (bng) {
                bng.DistanceFromGroundOffset = 0f;
            }

            // Add the CharacterController
            characterController = GetComponent<CharacterController>();
            if (characterController == null) {
                characterController = gameObject.AddComponent<CharacterController>();
                characterController.skinWidth = 0.001f;
                characterController.center = new Vector3(0, -0.25f, 0);
                characterController.radius = 0.1f;
                characterController.height = 1.5f;
            }

            playerInitialized = true;
        }

        public virtual void SetupRigidbodyPlayer() {
            playerRigid = gameObject.AddComponent<Rigidbody>();
            playerRigid.mass = 50f;
            playerRigid.drag = 1f;
            playerRigid.angularDrag = 0.05f;
            playerRigid.freezeRotation = true;
            // playerRigid.useGravity = false; // Gravity is applied manually

            // Remove any CharacterControllers, if any
            CharacterController charController = GetComponent<CharacterController>();
            if (charController != null) {
                GameObject.Destroy(charController);
            }

            BNGPlayerController bng = GetComponent<BNGPlayerController>();
            if (bng) {
                bng.DistanceFromGroundOffset = -0.087f;
            }

            // Add the colliders
            CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
            cc.radius = 0.1f;
            cc.center = new Vector3(0, 0.785f, 0);
            cc.height = 1.25f;

            playerSphere = gameObject.AddComponent<SphereCollider>();
            playerSphere.center = new Vector3(0, 0.079f, 0);
            playerSphere.radius = 0.08f;

            playerInitialized = true;
        }

        public virtual void EnableMovement() {
            UpdateMovement = true;
        }

        public virtual void DisableMovement() {
            UpdateMovement = false;
        }

        [Header("Shown for Debug : ")]
        public int GroundContacts = 0;
        public float SurfaceAngle = 0f;
        public float SurfaceHeight = 0f;

        //public virtual void OnCollisionEnter(Collision collision) {
        //    if(ControllerType == PlayerControllerType.Rigidbody) {
        //        // May want to check for sphere collider here, contact normals, etc. Keeping it simple for now.
        //        GroundContacts++;
        //    }
        //}
        //public virtual void OnCollisionExit(Collision collision) {
        //    if(ControllerType == PlayerControllerType.Rigidbody) {
        //        GroundContacts--;
        //    }
        //}
        
        void OnCollisionStay(Collision collisionInfo) {
            if (ControllerType == PlayerControllerType.Rigidbody) {
                //= collisionInfo.contacts.Length;

                for (int x = 0; x < collisionInfo.contacts.Length; x++) {
                    ContactPoint contact = collisionInfo.contacts[x];
                    SurfaceAngle = Vector3.Angle(contact.normal, transform.up);
                    SurfaceHeight = Mathf.Abs(contact.point.y - transform.position.y);

                    // Smooth floating point out for debug
                    if(SurfaceHeight < 0.0001) {
                        SurfaceHeight = 0;
                    }

                    // Is this a valid contact?
                    if(SurfaceHeight <= StepHeight) {
                        GroundContacts++;
                    }
                    else if (SurfaceAngle <= MaxSlopeAngle) {
                        GroundContacts++;
                    }

                    // Debug - draw all contact points and normals
                    //Debug.DrawRay(contact.point, contact.normal * 10, Color.white);

                    Debug.DrawLine(contact.point, transform.position);
                }
            }
        }
    }

    public enum PlayerControllerType {
        CharacterController,
        Rigidbody
    }
}

